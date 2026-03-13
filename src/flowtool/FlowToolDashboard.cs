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
	// 全部类布局作用域键。
	private const string allLayoutScopeKey = "all";
	// 自动保存节流秒数。
	private const double autoSaveIntervalSeconds = 0.25d;

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
	private FlowToolCanvasPanelController canvasPanel = null!;
	// 右侧未分配池组件。
	private UnassignedPoolPanel unassignedPoolPanel = null!;

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
		canvasPanel.UpdateDeleteButtonVisibility();
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
		layoutScopePanel = GetNode<ScopePanel>("SplitContainer/ScopePanel");
		canvasPanel = new FlowToolCanvasPanelController(
			GetNode<FlowToolCanvasGraphEdit>("SplitContainer/ContentSplitContainer/EditorPanel/Canvas"),
			onDeleteButtonPressed);
		unassignedPoolPanel = new UnassignedPoolPanel(
			GetNode<VBoxContainer>("SplitContainer/ContentSplitContainer/UnassignedPanel/PoolScrollContainer/UnassignedPoolList"),
			GetNode<Label>("SplitContainer/ContentSplitContainer/UnassignedPanel/StatusLabel"));
	}

	// 初始化画布行为与分栏自适应。
	private void configureUiBehavior()
	{
		canvasPanel.Configure(onNodePayloadDropped);
		layoutScopePanel.BindSelection(onLayoutScopeSelected);

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
		IReadOnlyCollection<string> validNodeIds = topology.Metrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);

		IReadOnlyDictionary<string, Vector2> persistedLayout = layoutStore.Load();
		layoutPositions = layoutStore.FilterInvalidNodes(persistedLayout, validNodeIds);
		activeNodeIds = deriveActiveNodeIds(layoutPositions.Keys);

		layoutScopePanel.Setup(layoutScopes, selectedLayoutScopeKey);
		canvasPanel.Render(topology, activeNodeIds, layoutPositions);
		unassignedPoolPanel.RenderPool(topology, activeNodeIds);
		persistLayoutCleanup();

		unassignedPoolPanel.SetStatus($"{statusText}\n布局: {selectedLayoutScopeDisplayName} | 指标节点: {topology.Metrics.Count} | 激活节点: {activeNodeIds.Count}");
	}

	// 切换布局作用域。
	private void onLayoutScopeSelected(long selectedIndex)
	{
		if (layoutScopePanel.IsUpdatingSelection)
		{
			return;
		}

		if (selectedIndex < 0 || selectedIndex >= layoutScopes.Count)
		{
			return;
		}

		FlowToolLayoutScopeItem selectedScope = layoutScopes[(int)selectedIndex];
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
	private void onNodePayloadDropped(string nodeId, string _, Vector2 graphPosition)
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

		canvasPanel.Render(topology, activeNodeIds, layoutPositions);
		unassignedPoolPanel.RenderPool(topology, activeNodeIds);
		autoSaveLayoutIfChanged(forceSave: true);
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

		canvasPanel.Render(topology, activeNodeIds, layoutPositions);
		unassignedPoolPanel.RenderPool(topology, activeNodeIds);
		autoSaveLayoutIfChanged(forceSave: true);
		unassignedPoolPanel.SetStatus($"已移回未分配池: {getNodeDisplayName(nodeId)}");
	}

	// 根据节点 ID 返回当前显示名。
	private string getNodeDisplayName(string nodeId)
	{
		FlowToolMetricNode? metricNode = topology.Metrics.FirstOrDefault(metric => metric.NodeId == nodeId);
		return metricNode?.DisplayName ?? nodeId;
	}

	// 自动保存布局变更。
	private void autoSaveLayoutIfChanged(bool forceSave = false)
	{
		IReadOnlyDictionary<string, Vector2> currentLayout = canvasPanel.CollectCurrentLayout();
		string currentFingerprint = createLayoutFingerprint(currentLayout);
		if (forceSave == false && currentFingerprint == lastLayoutFingerprint)
		{
			return;
		}

		layoutPositions = currentLayout;
		layoutStore.Save(layoutPositions);
		lastLayoutFingerprint = currentFingerprint;
		unassignedPoolPanel.SetStatus($"布局已自动保存: {DateTime.Now:HH:mm:ss}");
	}

	// 在重载布局作用域或拓扑前立即保存当前画布，避免被后续重绘覆盖。
	private void persistCurrentLayoutSnapshot()
	{
		if (canvasPanel.HasRenderedNodes == false)
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
}
