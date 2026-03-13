using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 系统动力学仪表盘编辑器入口。
/// </summary>
[Tool]
[GlobalClass]
public partial class FlowToolCanvas : Control
{
	/// <summary>
	/// 自动保存心跳信号。
	/// </summary>
	[Signal]
	public delegate void AutosavePulseEventHandler(double delta);

	/// <summary>
	/// 自动保存强制落库信号。
	/// </summary>
	[Signal]
	public delegate void AutosaveForcedEventHandler();

	/// <summary>
	/// 自动保存快照请求信号。
	/// </summary>
	[Signal]
	public delegate void AutosaveSnapshotRequestedEventHandler();

	/// <summary>
	/// 自动保存布局提交信号。
	/// </summary>
	[Signal]
	public delegate void AutosaveCommitLayoutEventHandler();

	/// <summary>
	/// 自动保存作用域切换信号。
	/// </summary>
	[Signal]
	public delegate void AutosaveScopeChangedEventHandler(string layoutScopeKey);

	// 全部类布局作用域键。
	private const string allLayoutScopeKey = "all";

	// 反射拓扑提取器。
	private readonly FlowToolTopologyExtractor topologyExtractor = new();
	// 布局存储器。
	private FlowToolLayoutStore layoutStore = new(allLayoutScopeKey);

	// 当前拓扑快照。
	private FlowToolTopology topology = new(Array.Empty<FlowToolMetricNode>(), Array.Empty<FlowToolEdge>());
	// 当前全部拓扑快照。
	private FlowToolTopology fullTopology = new(Array.Empty<FlowToolMetricNode>(), Array.Empty<FlowToolEdge>());
	// 当前布局坐标。
	private IReadOnlyDictionary<string, Vector2> layoutPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
	// 当前布局作用域键。
	private string selectedLayoutScopeKey = allLayoutScopeKey;
	// 当前布局作用域显示名。
	private string selectedLayoutScopeDisplayName = string.Empty;
	// 当前布局作用域列表。
	private IReadOnlyList<FlowToolLayoutScopeItem> layoutScopes = Array.Empty<FlowToolLayoutScopeItem>();
	// 当前已激活节点集合。
	private HashSet<string> activeNodeIds = new(StringComparer.Ordinal);

	// 左侧类列表与右侧内容分栏容器。
	private HSplitContainer splitContainer = null!;
	// 中央编辑区与右侧未分配池分栏容器。
	private HSplitContainer contentSplitContainer = null!;
	// 左侧类列表组件。
	private ScopePanel layoutScopePanel = null!;
	// 中央编辑画布组件。
	private FlowToolCanvasGraphEdit canvasPanel = null!;
	// 右侧未分配池组件。
	private UnassignedPoolPanel unassignedPoolPanel = null!;

	public override void _Ready()
	{
		bindUiFromScene();
		configureUiBehavior();
		reloadTopologyAndRender();
	}

	public override void _Process(double delta)
	{
		EmitSignal(SignalName.AutosavePulse, delta);
		canvasPanel.UpdateDeleteButtonVisibility();
	}

	public override void _ExitTree()
	{
		EmitSignal(SignalName.AutosaveForced);
	}

	// 从 tscn 场景树绑定所需节点。
	private void bindUiFromScene()
	{
		splitContainer = GetNode<HSplitContainer>("SplitContainer");
		contentSplitContainer = GetNode<HSplitContainer>("SplitContainer/ContentSplitContainer");
		layoutScopePanel = GetNode<ScopePanel>("SplitContainer/ScopePanel");
		canvasPanel = GetNode<FlowToolCanvasGraphEdit>("SplitContainer/ContentSplitContainer/EditorPanel/Canvas");
		unassignedPoolPanel = GetNode<UnassignedPoolPanel>("SplitContainer/ContentSplitContainer/UnassignedPoolPanel");
	}

	// 初始化画布行为与分栏自适应。
	private void configureUiBehavior()
	{
		canvasPanel.NodePayloadDropped += onNodePayloadDropped;
		canvasPanel.SetDeleteNodeRequested(onDeleteButtonPressed);
		canvasPanel.RightDisconnects = false;
		canvasPanel.ShowZoomLabel = true;
		canvasPanel.Zoom = 1f;
		canvasPanel.MinimapEnabled = true;
		canvasPanel.ShowArrangeButton = false;
		layoutScopePanel.ScopeSelected += onLayoutScopeSelected;

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
	private void reloadTopologyAndRender()
	{
		fullTopology = topologyExtractor.ExtractFromCurrentAssembly();
		rebuildLayoutScopes(fullTopology);
		topology = filterTopologyByLayoutScope(fullTopology, selectedLayoutScopeKey);
		IReadOnlyCollection<string> validNodeIds = topology.Metrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);

		IReadOnlyDictionary<string, Vector2> persistedLayout = layoutStore.Load();
		layoutPositions = layoutStore.FilterInvalidNodes(persistedLayout, validNodeIds);
		activeNodeIds = deriveActiveNodeIds(layoutPositions.Keys);

		layoutScopePanel.Update(layoutScopes, selectedLayoutScopeKey);
		canvasPanel.RenderTopology(topology, activeNodeIds, layoutPositions);
		unassignedPoolPanel.Update(topology, activeNodeIds);
		EmitSignal(SignalName.AutosaveScopeChanged, selectedLayoutScopeKey);
		EmitSignal(SignalName.AutosaveCommitLayout);
	}

	// 切换布局作用域。
	private void onLayoutScopeSelected(long selectedIndex)
	{
		if (selectedIndex < 0 || selectedIndex >= layoutScopes.Count)
		{
			return;
		}

		FlowToolLayoutScopeItem selectedScope = layoutScopes[(int)selectedIndex];
		if (selectedScope.ScopeKey == selectedLayoutScopeKey)
		{
			return;
		}

		EmitSignal(SignalName.AutosaveSnapshotRequested);
		selectedLayoutScopeKey = selectedScope.ScopeKey;
		selectedLayoutScopeDisplayName = selectedScope.DisplayName;
		layoutStore = new FlowToolLayoutStore(selectedLayoutScopeKey);
		reloadTopologyAndRender();
	}

	// 构建布局作用域列表。
	private void rebuildLayoutScopes(FlowToolTopology sourceTopology)
	{
		layoutScopes = sourceTopology.Metrics
			.Select(static metric => metric.OwnerTypeFullName)
			.Where(static ownerTypeName => string.IsNullOrWhiteSpace(ownerTypeName) == false)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static ownerTypeName => ownerTypeName, StringComparer.Ordinal)
			.Select(static ownerTypeName => new FlowToolLayoutScopeItem(ownerTypeName!, getTypeShortName(ownerTypeName!)))
			.ToList();

		if (layoutScopes.Any(scope => scope.ScopeKey == selectedLayoutScopeKey) == false)
		{
			FlowToolLayoutScopeItem? fallbackScope = layoutScopes.FirstOrDefault();
			selectedLayoutScopeKey = fallbackScope?.ScopeKey ?? allLayoutScopeKey;
			selectedLayoutScopeDisplayName = fallbackScope?.DisplayName ?? string.Empty;
			layoutStore = new FlowToolLayoutStore(selectedLayoutScopeKey);
		}
	}

	// 按布局作用域过滤拓扑。
	private static FlowToolTopology filterTopologyByLayoutScope(FlowToolTopology sourceTopology, string layoutScopeKey)
	{
		if (layoutScopeKey == allLayoutScopeKey)
		{
			return sourceTopology;
		}

		IReadOnlyList<FlowToolMetricNode> scopedMetrics = sourceTopology.Metrics
			.Where(metric => metric.OwnerTypeFullName == layoutScopeKey)
			.ToList();
		HashSet<string> scopedMetricNodeIds = scopedMetrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);

		IReadOnlyList<FlowToolEdge> scopedEdges = sourceTopology.Edges
			.Where(edge => scopedMetricNodeIds.Contains(edge.FromNodeId) && scopedMetricNodeIds.Contains(edge.ToNodeId))
			.ToList();

		return new FlowToolTopology(scopedMetrics, scopedEdges);
	}

	// 获取类型短名。
	private static string getTypeShortName(string fullTypeName)
	{
		if (string.IsNullOrWhiteSpace(fullTypeName))
		{
			return string.Empty;
		}

		string[] segments = fullTypeName.Split('.', StringSplitOptions.RemoveEmptyEntries);
		return segments.LastOrDefault() ?? fullTypeName;
	}

	// 根据布局恢复激活态，仅保留当前拓扑内的有效指标。
	private HashSet<string> deriveActiveNodeIds(IEnumerable<string> layoutNodeIds)
	{
		HashSet<string> validMetricNodeIds = topology.Metrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);
		HashSet<string> activeIds = layoutNodeIds
			.Where(validMetricNodeIds.Contains)
			.ToHashSet(StringComparer.Ordinal);

		return activeIds;
	}

	// 将拖拽节点加入画布并触发自动连线。
	private void onNodePayloadDropped(string nodeId, Vector2 graphPosition)
	{
		bool isNewlyActivated = activeNodeIds.Add(nodeId);
		if (isNewlyActivated == false)
		{
			return;
		}

		Dictionary<string, Vector2> nextLayout = activeNodeIds
			.ToDictionary(
				node => node,
				node => layoutPositions.TryGetValue(node, out Vector2 existingPosition) ? existingPosition : graphPosition,
				StringComparer.Ordinal
			);

		nextLayout[nodeId] = graphPosition;
		layoutPositions = nextLayout;

		canvasPanel.RenderTopology(topology, activeNodeIds, layoutPositions);
		unassignedPoolPanel.Update(topology, activeNodeIds);
		EmitSignal(SignalName.AutosaveForced);
	}

	// 删除节点并将其回收到未分配池，同时清理失去依附的指标节点。
	private void onDeleteButtonPressed(string nodeId)
	{
		activeNodeIds = activeNodeIds
			.Where(activeNodeId => activeNodeId != nodeId)
			.ToHashSet(StringComparer.Ordinal);
		layoutPositions = layoutPositions
			.Where(pair => activeNodeIds.Contains(pair.Key))
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);

		canvasPanel.RenderTopology(topology, activeNodeIds, layoutPositions);
		unassignedPoolPanel.Update(topology, activeNodeIds);
		EmitSignal(SignalName.AutosaveForced);
	}

}
