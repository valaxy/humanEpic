using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// 系统动力学仪表盘编辑器入口。
/// </summary>
[Tool]
[GlobalClass]
public partial class FlowToolDashboard : Control
{
	// 过程节点类型标识。
	private const string processNodeKind = "process";
	// 指标节点类型标识。
	private const string metricNodeKind = "metric";
	// 自动保存节流秒数。
	private const double autoSaveIntervalSeconds = 0.25d;

	// 反射拓扑提取器。
	private readonly FlowToolTopologyExtractor topologyExtractor = new();
	// 布局存储器。
	private readonly FlowToolLayoutStore layoutStore = new();

	// 当前拓扑快照。
	private FlowToolTopology topology = new(Array.Empty<FlowToolProcessNode>(), Array.Empty<FlowToolMetricNode>(), Array.Empty<FlowToolEdge>());
	// 当前布局坐标。
	private IReadOnlyDictionary<string, Vector2> layoutPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
	// 当前已激活节点集合。
	private HashSet<string> activeNodeIds = new(StringComparer.Ordinal);
	// 当前画布节点映射。
	private Dictionary<string, GraphNode> activeGraphNodes = new(StringComparer.Ordinal);
	// 当前画布节点名映射。
	private Dictionary<string, string> graphNodeNameByNodeId = new(StringComparer.Ordinal);

	// 左侧画布。
	private FlowToolCanvasGraphEdit canvas = null!;
	// 左右分栏容器。
	private HSplitContainer splitContainer = null!;
	// 右侧未分配池列表。
	private VBoxContainer unassignedPoolList = null!;
	// 状态栏文本。
	private Label statusLabel = null!;

	// 自动保存计时器。
	private double saveClockSeconds;
	// 最近一次布局指纹。
	private string lastLayoutFingerprint = string.Empty;

	public override void _Ready()
	{
		bindUiFromScene();
		configureUiBehavior();
		reloadTopologyAndRender("已完成初次反射提取");
	}

	public override void _Process(double delta)
	{
		saveClockSeconds += delta;
		if (saveClockSeconds < autoSaveIntervalSeconds)
		{
			return;
		}

		saveClockSeconds = 0d;
		autoSaveLayoutIfChanged();
	}

	// 从 tscn 场景树绑定所需节点。
	private void bindUiFromScene()
	{
		splitContainer = GetNode<HSplitContainer>("SplitContainer");
		canvas = GetNode<FlowToolCanvasGraphEdit>("SplitContainer/Canvas");
		unassignedPoolList = GetNode<VBoxContainer>("SplitContainer/UnassignedPanel/PoolScrollContainer/UnassignedPoolList");
		statusLabel = GetNode<Label>("SplitContainer/UnassignedPanel/StatusLabel");

		Button refreshButton = GetNode<Button>("SplitContainer/UnassignedPanel/RefreshButton");
		refreshButton.Pressed += onRefreshButtonPressed;
	}

	// 初始化画布行为与分栏自适应。
	private void configureUiBehavior()
	{
		canvas.NodePayloadDropped += onNodePayloadDropped;
		canvas.RightDisconnects = false;
		canvas.ShowZoomLabel = true;
		canvas.Zoom = 1f;
		canvas.MinimapEnabled = true;

		applyAdaptiveSplitOffset();
		Resized += onDashboardResized;
	}

	// 根节点尺寸变化时更新分栏比例，避免窗口变化后内容挤压。
	private void onDashboardResized()
	{
		applyAdaptiveSplitOffset();
	}

	// 按当前窗口宽度计算分栏位置。
	private void applyAdaptiveSplitOffset()
	{
		float safeWidth = Mathf.Max(Size.X, 640f);
		float desiredOffset = safeWidth * 0.68f;
		splitContainer.SplitOffsets = [Mathf.RoundToInt(desiredOffset)];
	}

	// 点击按钮后重新提取拓扑并重绘。
	private void onRefreshButtonPressed()
	{
		reloadTopologyAndRender("已重新提取关系");
	}

	// 执行提取、状态合并与重绘。
	private void reloadTopologyAndRender(string statusText)
	{
		topology = topologyExtractor.ExtractFromCurrentAssembly();
		IReadOnlyCollection<string> validNodeIds = topology.Processes
			.Select(static process => process.NodeId)
			.Concat(topology.Metrics.Select(static metric => metric.NodeId))
			.ToHashSet(StringComparer.Ordinal);

		IReadOnlyDictionary<string, Vector2> persistedLayout = layoutStore.Load();
		layoutPositions = layoutStore.FilterInvalidNodes(persistedLayout, validNodeIds);
		activeNodeIds = deriveActiveNodeIds(layoutPositions.Keys);
		seedInitialActiveNodesWhenEmpty();

		renderCanvasAndConnections();
		renderUnassignedPool();
		persistLayoutCleanup();

		statusLabel.Text = $"{statusText}\n过程节点: {topology.Processes.Count} | 指标节点: {topology.Metrics.Count} | 激活节点: {activeNodeIds.Count}";
	}

	// 根据布局恢复激活态，并补齐已激活过程关联指标。
	private HashSet<string> deriveActiveNodeIds(IEnumerable<string> layoutNodeIds)
	{
		HashSet<string> layoutIdSet = layoutNodeIds.ToHashSet(StringComparer.Ordinal);
		HashSet<string> processIdsInLayout = layoutIdSet
			.Where(static nodeId => nodeId.StartsWith("process:", StringComparison.Ordinal))
			.ToHashSet(StringComparer.Ordinal);

		HashSet<string> processRelatedMetricIds = topology.Edges
			.Where(edge => processIdsInLayout.Contains(edge.FromNodeId) || processIdsInLayout.Contains(edge.ToNodeId))
			.SelectMany(static edge => new[] { edge.FromNodeId, edge.ToNodeId })
			.Where(static nodeId => nodeId.StartsWith("metric:", StringComparison.Ordinal))
			.ToHashSet(StringComparer.Ordinal);

		HashSet<string> activeIds = layoutIdSet
			.Union(processRelatedMetricIds)
			.ToHashSet(StringComparer.Ordinal);

		return activeIds;
	}

	// 渲染左侧画布节点与自动连线。
	private void renderCanvasAndConnections()
	{
		activeGraphNodes.Values.ToList().ForEach(static node => node.QueueFree());
		activeGraphNodes = new Dictionary<string, GraphNode>(StringComparer.Ordinal);
		graphNodeNameByNodeId = new Dictionary<string, string>(StringComparer.Ordinal);

		IReadOnlyDictionary<string, FlowToolProcessNode> processById = topology.Processes.ToDictionary(static process => process.NodeId, StringComparer.Ordinal);
		IReadOnlyDictionary<string, FlowToolMetricNode> metricById = topology.Metrics.ToDictionary(static metric => metric.NodeId, StringComparer.Ordinal);

		IReadOnlyList<FlowToolVisualNodeDescriptor> descriptors = activeNodeIds
			.Select(nodeId => createVisualDescriptor(nodeId, processById, metricById))
			.Where(static descriptor => descriptor is not null)
			.Select(static descriptor => descriptor!)
			.OrderBy(static descriptor => descriptor.Kind, StringComparer.Ordinal)
			.ThenBy(static descriptor => descriptor.DisplayName, StringComparer.Ordinal)
			.ToList();

		descriptors
			.Select(createGraphNode)
			.ToList()
			.ForEach(node => canvas.AddChild(node));

		canvas.ClearConnections();
		topology.Edges
			.Where(edge => activeNodeIds.Contains(edge.FromNodeId) && activeNodeIds.Contains(edge.ToNodeId))
			.ToList()
			.ForEach(connectEdgeIfPossible);
	}

	// 渲染右侧未分配池。
	private void renderUnassignedPool()
	{
		unassignedPoolList.GetChildren().ToList().ForEach(static child => child.QueueFree());

		IReadOnlyList<FlowToolPoolItemButton> processItems = topology.Processes
			.Where(process => activeNodeIds.Contains(process.NodeId) == false)
			.OrderBy(static process => process.DisplayName, StringComparer.Ordinal)
			.Select(createProcessPoolItem)
			.ToList();

		IReadOnlyList<FlowToolPoolItemButton> metricItems = topology.Metrics
			.Where(metric => activeNodeIds.Contains(metric.NodeId) == false)
			.OrderBy(static metric => metric.DisplayName, StringComparer.Ordinal)
			.Select(createMetricPoolItem)
			.ToList();

		processItems.Concat(metricItems).ToList().ForEach(item => unassignedPoolList.AddChild(item));
	}

	// 将拖拽节点加入画布并触发自动连线。
	private void onNodePayloadDropped(string nodeId, string nodeKind, Vector2 graphPosition)
	{
		bool isNewlyActivated = activeNodeIds.Add(nodeId);
		if (isNewlyActivated == false)
		{
			return;
		}

		IReadOnlyList<string> relatedMetricIds = nodeKind == processNodeKind
			? topology.Edges
				.Where(edge => edge.FromNodeId == nodeId || edge.ToNodeId == nodeId)
				.SelectMany(static edge => new[] { edge.FromNodeId, edge.ToNodeId })
				.Where(static relatedNodeId => relatedNodeId.StartsWith("metric:", StringComparison.Ordinal))
				.Distinct(StringComparer.Ordinal)
				.ToList()
			: Array.Empty<string>();
		relatedMetricIds.ToList().ForEach(relatedMetricId => activeNodeIds.Add(relatedMetricId));

		Dictionary<string, Vector2> nextLayout = activeNodeIds
			.ToDictionary(
				node => node,
				node => layoutPositions.TryGetValue(node, out Vector2 existingPosition) ? existingPosition : graphPosition,
				StringComparer.Ordinal
			);

		nextLayout[nodeId] = graphPosition;
		applyDefaultMetricPositionsForProcessDrop(nextLayout, nodeId, relatedMetricIds, graphPosition);
		layoutPositions = nextLayout;

		renderCanvasAndConnections();
		renderUnassignedPool();
		autoSaveLayoutIfChanged(forceSave: true);
	}

	// 在首次打开且无布局时，自动激活一个过程与相关指标，避免空画布无反馈。
	private void seedInitialActiveNodesWhenEmpty()
	{
		if (activeNodeIds.Count > 0)
		{
			return;
		}

		FlowToolProcessNode? seedProcess = topology.Processes
			.OrderBy(static process => process.DisplayName, StringComparer.Ordinal)
			.FirstOrDefault();
		if (seedProcess is null)
		{
			return;
		}

		activeNodeIds.Add(seedProcess.NodeId);

		IReadOnlyList<string> inputMetricIds = topology.Edges
			.Where(edge => edge.ToNodeId == seedProcess.NodeId && edge.FromNodeId.StartsWith("metric:", StringComparison.Ordinal))
			.Select(static edge => edge.FromNodeId)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static metricId => metricId, StringComparer.Ordinal)
			.ToList();
		IReadOnlyList<string> outputMetricIds = topology.Edges
			.Where(edge => edge.FromNodeId == seedProcess.NodeId && edge.ToNodeId.StartsWith("metric:", StringComparison.Ordinal))
			.Select(static edge => edge.ToNodeId)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static metricId => metricId, StringComparer.Ordinal)
			.ToList();

		inputMetricIds.Concat(outputMetricIds).ToList().ForEach(metricId => activeNodeIds.Add(metricId));

		Vector2 seedProcessPosition = new(260f, 200f);
		Dictionary<string, Vector2> seededLayout = layoutPositions.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		seededLayout[seedProcess.NodeId] = seedProcessPosition;

		inputMetricIds
			.Select((metricId, index) => new { metricId, index })
			.ToList()
			.ForEach(item => seededLayout[item.metricId] = new Vector2(30f, 120f + (item.index * 110f)));
		outputMetricIds
			.Select((metricId, index) => new { metricId, index })
			.ToList()
			.ForEach(item => seededLayout[item.metricId] = new Vector2(520f, 160f + (item.index * 110f)));

		layoutPositions = seededLayout;
	}

	// 为刚激活的过程补齐默认指标位置，防止全部重叠在同一点。
	private void applyDefaultMetricPositionsForProcessDrop(
		Dictionary<string, Vector2> nextLayout,
		string processNodeId,
		IReadOnlyList<string> relatedMetricIds,
		Vector2 processPosition)
	{
		if (processNodeId.StartsWith("process:", StringComparison.Ordinal) == false)
		{
			return;
		}

		IReadOnlyList<string> inputMetricIds = topology.Edges
			.Where(edge => edge.ToNodeId == processNodeId && relatedMetricIds.Contains(edge.FromNodeId))
			.Select(static edge => edge.FromNodeId)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static metricId => metricId, StringComparer.Ordinal)
			.ToList();
		IReadOnlyList<string> outputMetricIds = topology.Edges
			.Where(edge => edge.FromNodeId == processNodeId && relatedMetricIds.Contains(edge.ToNodeId))
			.Select(static edge => edge.ToNodeId)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static metricId => metricId, StringComparer.Ordinal)
			.ToList();

		inputMetricIds
			.Where(metricId => layoutPositions.ContainsKey(metricId) == false)
			.Select((metricId, index) => new { metricId, index })
			.ToList()
			.ForEach(item => nextLayout[item.metricId] = processPosition + new Vector2(-240f, -80f + (item.index * 90f)));
		outputMetricIds
			.Where(metricId => layoutPositions.ContainsKey(metricId) == false)
			.Select((metricId, index) => new { metricId, index })
			.ToList()
			.ForEach(item => nextLayout[item.metricId] = processPosition + new Vector2(260f, -40f + (item.index * 90f)));
	}

	// 创建过程池项。
	private static FlowToolPoolItemButton createProcessPoolItem(FlowToolProcessNode processNode)
	{
		FlowToolPoolItemButton button = new();
		button.Setup($"[过程] {processNode.DisplayName}", processNode.NodeId, processNodeKind);
		return button;
	}

	// 创建指标池项。
	private static FlowToolPoolItemButton createMetricPoolItem(FlowToolMetricNode metricNode)
	{
		FlowToolPoolItemButton button = new();
		button.Setup($"[指标] {metricNode.DisplayName}", metricNode.NodeId, metricNodeKind);
		return button;
	}

	// 构建可视节点描述。
	private static FlowToolVisualNodeDescriptor? createVisualDescriptor(
		string nodeId,
		IReadOnlyDictionary<string, FlowToolProcessNode> processById,
		IReadOnlyDictionary<string, FlowToolMetricNode> metricById)
	{
		if (processById.TryGetValue(nodeId, out FlowToolProcessNode? processNode))
		{
			return new FlowToolVisualNodeDescriptor(processNode.NodeId, processNode.DisplayName, processNodeKind);
		}

		if (metricById.TryGetValue(nodeId, out FlowToolMetricNode? metricNode))
		{
			return new FlowToolVisualNodeDescriptor(metricNode.NodeId, metricNode.DisplayName, metricNodeKind);
		}

		return null;
	}

	// 创建 GraphNode。
	private GraphNode createGraphNode(FlowToolVisualNodeDescriptor descriptor)
	{
		string nodeName = $"Node_{activeGraphNodes.Count.ToString(CultureInfo.InvariantCulture)}";
		graphNodeNameByNodeId[descriptor.NodeId] = nodeName;

		GraphNode graphNode = new()
		{
			Name = nodeName,
			Title = descriptor.DisplayName,
			PositionOffset = layoutPositions.TryGetValue(descriptor.NodeId, out Vector2 position) ? position : new Vector2(80f, 80f),
			Resizable = false,
			Draggable = true,
			Size = descriptor.Kind == processNodeKind ? new Vector2(240f, 90f) : new Vector2(220f, 70f)
		};

		Label body = new()
		{
			Text = descriptor.Kind == processNodeKind ? "过程节点" : "指标节点",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		graphNode.AddChild(body);

		Color portColor = descriptor.Kind == processNodeKind ? new Color(0.92f, 0.69f, 0.39f) : new Color(0.39f, 0.69f, 0.92f);
		graphNode.SetSlot(0, true, 0, portColor, true, 0, portColor);
		applyNodeStyle(graphNode, descriptor.Kind);

		activeGraphNodes[descriptor.NodeId] = graphNode;
		return graphNode;
	}

	// 根据节点类型设置外观。
	private static void applyNodeStyle(GraphNode graphNode, string nodeKind)
	{
		StyleBoxFlat panelStyle = new()
		{
			BgColor = nodeKind == processNodeKind ? new Color(0.18f, 0.14f, 0.1f) : new Color(0.12f, 0.16f, 0.2f),
			BorderColor = nodeKind == processNodeKind ? new Color(0.92f, 0.69f, 0.39f) : new Color(0.39f, 0.69f, 0.92f),
			BorderWidthBottom = 2,
			BorderWidthTop = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			CornerRadiusTopLeft = nodeKind == processNodeKind ? 12 : 0,
			CornerRadiusTopRight = nodeKind == processNodeKind ? 12 : 0,
			CornerRadiusBottomLeft = nodeKind == processNodeKind ? 12 : 0,
			CornerRadiusBottomRight = nodeKind == processNodeKind ? 12 : 0
		};
		graphNode.AddThemeStyleboxOverride("panel", panelStyle);
	}

	// 建立自动连线。
	private void connectEdgeIfPossible(FlowToolEdge edge)
	{
		bool hasFromNode = graphNodeNameByNodeId.TryGetValue(edge.FromNodeId, out string? fromNodeName);
		bool hasToNode = graphNodeNameByNodeId.TryGetValue(edge.ToNodeId, out string? toNodeName);
		if (hasFromNode == false || hasToNode == false)
		{
			return;
		}

		if (canvas.IsNodeConnected(fromNodeName!, 0, toNodeName!, 0))
		{
			return;
		}

		canvas.ConnectNode(fromNodeName!, 0, toNodeName!, 0);
	}

	// 自动保存布局变更。
	private void autoSaveLayoutIfChanged(bool forceSave = false)
	{
		IReadOnlyDictionary<string, Vector2> currentLayout = activeGraphNodes
			.ToDictionary(static pair => pair.Key, static pair => pair.Value.PositionOffset, StringComparer.Ordinal);
		string currentFingerprint = createLayoutFingerprint(currentLayout);
		if (forceSave == false && currentFingerprint == lastLayoutFingerprint)
		{
			return;
		}

		layoutPositions = currentLayout;
		layoutStore.Save(layoutPositions);
		lastLayoutFingerprint = currentFingerprint;
		statusLabel.Text = $"布局已自动保存: {DateTime.Now:HH:mm:ss}";
	}

	// 保存一次过滤后的布局，清理失效节点坐标。
	private void persistLayoutCleanup()
	{
		lastLayoutFingerprint = createLayoutFingerprint(layoutPositions);
		layoutStore.Save(layoutPositions);
	}

	// 生成布局变更检测指纹。
	private static string createLayoutFingerprint(IReadOnlyDictionary<string, Vector2> nodePositions)
	{
		IReadOnlyList<string> tokens = nodePositions
			.OrderBy(static pair => pair.Key, StringComparer.Ordinal)
			.Select(static pair => $"{pair.Key}:{pair.Value.X.ToString("F2", CultureInfo.InvariantCulture)},{pair.Value.Y.ToString("F2", CultureInfo.InvariantCulture)}")
			.ToList();
		return string.Join("|", tokens);
	}

	// 画布可视节点描述。
	private sealed record FlowToolVisualNodeDescriptor(string NodeId, string DisplayName, string Kind);
}
