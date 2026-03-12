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
	// 全部类布局作用域键。
	private const string allLayoutScopeKey = "all";
	// 自动保存节流秒数。
	private const double autoSaveIntervalSeconds = 0.25d;

	// 反射拓扑提取器。
	private readonly FlowToolTopologyExtractor topologyExtractor = new();
	// 布局存储器。
	private FlowToolLayoutStore layoutStore = new(allLayoutScopeKey);

	// 当前拓扑快照。
	private FlowToolTopology topology = new(Array.Empty<FlowToolProcessNode>(), Array.Empty<FlowToolMetricNode>(), Array.Empty<FlowToolEdge>());
	// 当前全部拓扑快照。
	private FlowToolTopology fullTopology = new(Array.Empty<FlowToolProcessNode>(), Array.Empty<FlowToolMetricNode>(), Array.Empty<FlowToolEdge>());
	// 当前布局坐标。
	private IReadOnlyDictionary<string, Vector2> layoutPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
	// 当前布局作用域键。
	private string selectedLayoutScopeKey = allLayoutScopeKey;
	// 当前布局作用域显示名。
	private string selectedLayoutScopeDisplayName = string.Empty;
	// 布局作用域列表更新锁。
	private bool isUpdatingLayoutScopeList;
	// 当前布局作用域列表。
	private IReadOnlyList<FlowToolLayoutScope> layoutScopes = Array.Empty<FlowToolLayoutScope>();
	// 当前已激活节点集合。
	private HashSet<string> activeNodeIds = new(StringComparer.Ordinal);
	// 当前画布节点映射。
	private Dictionary<string, GraphNode> activeGraphNodes = new(StringComparer.Ordinal);
	// 当前画布节点名映射。
	private Dictionary<string, string> graphNodeNameByNodeId = new(StringComparer.Ordinal);
	// 当前节点删除按钮映射。
	private Dictionary<string, Button> deleteButtonByNodeId = new(StringComparer.Ordinal);

	// 左侧画布。
	private FlowToolCanvasGraphEdit canvas = null!;
	// 左侧类布局列表。
	private ItemList layoutScopeList = null!;
	// 左侧类列表与右侧内容分栏容器。
	private HSplitContainer splitContainer = null!;
	// 中央编辑区与右侧未分配池分栏容器。
	private HSplitContainer contentSplitContainer = null!;
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
		updateDeleteButtonVisibility();
	}

	public override void _ExitTree()
	{
		autoSaveLayoutIfChanged(forceSave: true);
	}

	// 从 tscn 场景树绑定所需节点。
	private void bindUiFromScene()
	{
		splitContainer = GetNode<HSplitContainer>("SplitContainer");
		contentSplitContainer = GetNode<HSplitContainer>("SplitContainer/ContentSplitContainer");
		canvas = GetNode<FlowToolCanvasGraphEdit>("SplitContainer/ContentSplitContainer/EditorPanel/Canvas");
		layoutScopeList = GetNode<ItemList>("SplitContainer/ClassPanel/LayoutScopeList");
		unassignedPoolList = GetNode<VBoxContainer>("SplitContainer/ContentSplitContainer/UnassignedPanel/PoolScrollContainer/UnassignedPoolList");
		statusLabel = GetNode<Label>("SplitContainer/ContentSplitContainer/UnassignedPanel/StatusLabel");
	}

	// 初始化画布行为与分栏自适应。
	private void configureUiBehavior()
	{
		canvas.NodePayloadDropped += onNodePayloadDropped;
		layoutScopeList.ItemSelected += onLayoutScopeSelected;
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
		float classPanelWidth = Mathf.Clamp(safeWidth * 0.22f, 220f, 320f);
		float editorPanelWidth = Mathf.Clamp(safeWidth * 0.5f, 420f, safeWidth - classPanelWidth - 280f);
		splitContainer.SplitOffsets = [Mathf.RoundToInt(classPanelWidth)];
		contentSplitContainer.SplitOffsets = [Mathf.RoundToInt(editorPanelWidth)];
	}

	// 执行提取、状态合并与重绘。
	private void reloadTopologyAndRender(string statusText)
	{
		fullTopology = topologyExtractor.ExtractFromCurrentAssembly();
		rebuildLayoutScopes(fullTopology);
		topology = filterTopologyByLayoutScope(fullTopology, selectedLayoutScopeKey);
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

		statusLabel.Text = $"{statusText}\n布局: {selectedLayoutScopeDisplayName} | 过程节点: {topology.Processes.Count} | 指标节点: {topology.Metrics.Count} | 激活节点: {activeNodeIds.Count}";
	}

	// 切换布局作用域。
	private void onLayoutScopeSelected(long selectedIndex)
	{
		if (isUpdatingLayoutScopeList)
		{
			return;
		}

		if (selectedIndex < 0 || selectedIndex >= layoutScopes.Count)
		{
			return;
		}

		FlowToolLayoutScope selectedScope = layoutScopes[(int)selectedIndex];
		if (selectedScope.ScopeKey == selectedLayoutScopeKey)
		{
			return;
		}

		persistCurrentLayoutSnapshot();
		selectedLayoutScopeKey = selectedScope.ScopeKey;
		selectedLayoutScopeDisplayName = selectedScope.DisplayName;
		layoutStore = new FlowToolLayoutStore(selectedLayoutScopeKey);
		reloadTopologyAndRender($"已切换到布局: {selectedLayoutScopeDisplayName}");
	}

	// 构建布局作用域列表。
	private void rebuildLayoutScopes(FlowToolTopology sourceTopology)
	{
		layoutScopes = sourceTopology.Processes
			.Select(static process => getProcessOwnerTypeName(process.NodeId))
			.Where(static ownerTypeName => string.IsNullOrWhiteSpace(ownerTypeName) == false)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static ownerTypeName => ownerTypeName, StringComparer.Ordinal)
			.Select(static ownerTypeName => new FlowToolLayoutScope(ownerTypeName!, ownerTypeName!.Split('.').Last()))
			.ToList();

		if (layoutScopes.Any(scope => scope.ScopeKey == selectedLayoutScopeKey) == false)
		{
			FlowToolLayoutScope? fallbackScope = layoutScopes.FirstOrDefault();
			selectedLayoutScopeKey = fallbackScope?.ScopeKey ?? allLayoutScopeKey;
			selectedLayoutScopeDisplayName = fallbackScope?.DisplayName ?? string.Empty;
			layoutStore = new FlowToolLayoutStore(selectedLayoutScopeKey);
		}

		isUpdatingLayoutScopeList = true;
		layoutScopeList.Clear();
		layoutScopes
			.Select(static scope => scope.DisplayName)
			.ToList()
			.ForEach(displayName => layoutScopeList.AddItem(displayName));

		int selectedScopeIndex = layoutScopes
			.Select((scope, index) => new { scope, index })
			.Where(item => item.scope.ScopeKey == selectedLayoutScopeKey)
			.Select(static item => item.index)
			.DefaultIfEmpty(0)
			.First();
		layoutScopeList.Select(selectedScopeIndex);
		isUpdatingLayoutScopeList = false;
	}

	// 按布局作用域过滤拓扑。
	private static FlowToolTopology filterTopologyByLayoutScope(FlowToolTopology sourceTopology, string layoutScopeKey)
	{
		if (layoutScopeKey == allLayoutScopeKey)
		{
			return sourceTopology;
		}

		IReadOnlyList<FlowToolProcessNode> scopedProcesses = sourceTopology.Processes
			.Where(process => getProcessOwnerTypeName(process.NodeId) == layoutScopeKey)
			.ToList();
		HashSet<string> processNodeIdSet = scopedProcesses
			.Select(static process => process.NodeId)
			.ToHashSet(StringComparer.Ordinal);

		IReadOnlyList<FlowToolEdge> scopedEdges = sourceTopology.Edges
			.Where(edge => processNodeIdSet.Contains(edge.FromNodeId) || processNodeIdSet.Contains(edge.ToNodeId))
			.ToList();
		HashSet<string> metricNodeIdSet = scopedEdges
			.SelectMany(static edge => new[] { edge.FromNodeId, edge.ToNodeId })
			.Where(static nodeId => nodeId.StartsWith("metric:", StringComparison.Ordinal))
			.ToHashSet(StringComparer.Ordinal);

		IReadOnlyList<FlowToolMetricNode> scopedMetrics = sourceTopology.Metrics
			.Where(metric => metricNodeIdSet.Contains(metric.NodeId))
			.ToList();

		return new FlowToolTopology(scopedProcesses, scopedMetrics, scopedEdges);
	}

	// 从过程节点 ID 提取所属类全名。
	private static string? getProcessOwnerTypeName(string processNodeId)
	{
		if (processNodeId.StartsWith("process:", StringComparison.Ordinal) == false)
		{
			return null;
		}

		string processToken = processNodeId["process:".Length..];
		int separatorIndex = processToken.LastIndexOf(".", StringComparison.Ordinal);
		if (separatorIndex <= 0)
		{
			return null;
		}

		return processToken[..separatorIndex];
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
		deleteButtonByNodeId = new Dictionary<string, Button>(StringComparer.Ordinal);

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
			return new FlowToolVisualNodeDescriptor(processNode.NodeId, processNode.DisplayName, processNodeKind, "过程节点");
		}

		if (metricById.TryGetValue(nodeId, out FlowToolMetricNode? metricNode))
		{
			return new FlowToolVisualNodeDescriptor(metricNode.NodeId, metricNode.DisplayName, metricNodeKind, $"参数类型: {metricNode.TypeDisplayName}");
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
			Title = string.Empty,
			PositionOffset = layoutPositions.TryGetValue(descriptor.NodeId, out Vector2 position) ? position : new Vector2(80f, 80f),
			Resizable = false,
			Draggable = true,
			CustomMinimumSize = new Vector2(descriptor.Kind == processNodeKind ? 240f : 220f, 0f)
		};
		graphNode.GetTitlebarHBox().Visible = false;

		VBoxContainer body = new()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		HBoxContainer header = new()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Label titleLabel = new()
		{
			Text = descriptor.DisplayName,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		Button deleteButton = createDeleteButton(descriptor.NodeId);
		Label kindLabel = new()
		{
			Text = descriptor.DetailText,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		titleLabel.SizeFlagsStretchRatio = 1f;
		header.AddChild(titleLabel);
		header.AddChild(deleteButton);
		body.AddChild(header);
		body.AddChild(kindLabel);
		graphNode.AddChild(body);

		Color portColor = descriptor.Kind == processNodeKind ? new Color(0.92f, 0.69f, 0.39f) : new Color(0.39f, 0.69f, 0.92f);
		graphNode.SetSlot(0, true, 0, portColor, true, 0, portColor);
		applyNodeStyle(graphNode, descriptor.Kind);
		graphNode.ResetSize();

		activeGraphNodes[descriptor.NodeId] = graphNode;
		return graphNode;
	}

	// 创建仅在选中时显示的删除按钮。
	private Button createDeleteButton(string nodeId)
	{
		Button deleteButton = new()
		{
			Text = "删除",
			Visible = false,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		deleteButton.Pressed += () => onDeleteButtonPressed(nodeId);
		deleteButtonByNodeId[nodeId] = deleteButton;
		return deleteButton;
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

	// 根据当前选中状态切换删除按钮可见性。
	private void updateDeleteButtonVisibility()
	{
		deleteButtonByNodeId
			.ToList()
			.ForEach(pair => pair.Value.Visible = activeGraphNodes.TryGetValue(pair.Key, out GraphNode? graphNode) && graphNode.Selected);
	}

	// 删除节点并将其回收到未分配池，同时清理失去依附的指标节点。
	private void onDeleteButtonPressed(string nodeId)
	{
		HashSet<string> remainingNodeIds = activeNodeIds
			.Where(activeNodeId => activeNodeId != nodeId)
			.ToHashSet(StringComparer.Ordinal);
		HashSet<string> remainingProcessNodeIds = remainingNodeIds
			.Where(static activeNodeId => activeNodeId.StartsWith("process:", StringComparison.Ordinal))
			.ToHashSet(StringComparer.Ordinal);
		HashSet<string> retainedMetricNodeIds = topology.Edges
			.Where(edge => remainingProcessNodeIds.Contains(edge.FromNodeId) || remainingProcessNodeIds.Contains(edge.ToNodeId))
			.SelectMany(static edge => new[] { edge.FromNodeId, edge.ToNodeId })
			.Where(static activeNodeId => activeNodeId.StartsWith("metric:", StringComparison.Ordinal))
			.ToHashSet(StringComparer.Ordinal);

		activeNodeIds = remainingNodeIds
			.Where(activeNodeId => activeNodeId.StartsWith("process:", StringComparison.Ordinal) || retainedMetricNodeIds.Contains(activeNodeId))
			.ToHashSet(StringComparer.Ordinal);
		layoutPositions = layoutPositions
			.Where(pair => activeNodeIds.Contains(pair.Key))
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);

		renderCanvasAndConnections();
		renderUnassignedPool();
		autoSaveLayoutIfChanged(forceSave: true);
		statusLabel.Text = $"已移回未分配池: {getNodeDisplayName(nodeId)}";
	}

	// 根据节点 ID 返回当前显示名。
	private string getNodeDisplayName(string nodeId)
	{
		FlowToolProcessNode? processNode = topology.Processes.FirstOrDefault(process => process.NodeId == nodeId);
		if (processNode is not null)
		{
			return processNode.DisplayName;
		}

		FlowToolMetricNode? metricNode = topology.Metrics.FirstOrDefault(metric => metric.NodeId == nodeId);
		return metricNode?.DisplayName ?? nodeId;
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

	// 在重载布局作用域或拓扑前立即保存当前画布，避免被后续重绘覆盖。
	private void persistCurrentLayoutSnapshot()
	{
		if (activeGraphNodes.Count == 0)
		{
			return;
		}

		autoSaveLayoutIfChanged(forceSave: true);
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
	private sealed record FlowToolVisualNodeDescriptor(string NodeId, string DisplayName, string Kind, string DetailText);

	// 布局作用域定义。
	private sealed record FlowToolLayoutScope(string ScopeKey, string DisplayName);
}
