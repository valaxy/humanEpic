using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 系统动力学仪表盘编辑器入口。
/// </summary>
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

	// 全部布局作用域键。
	private const string allLayoutScopeKey = "all";
	// 拓扑提取器。
	private readonly TopologyExtractor topologyExtractor = new();
	// 布局存储。
	private FlowToolLayoutStore layoutStore = new(allLayoutScopeKey);
	// 当前作用域拓扑。
	private WorldCanvas topology = WorldCanvas.Empty;
	// 当前布局坐标。
	private IReadOnlyDictionary<string, Vector2> layoutPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
	// 当前布局作用域。
	private string selectedLayoutScopeKey = allLayoutScopeKey;
	// 当前布局作用域列表。
	private IReadOnlyList<FlowToolLayoutScopeItem> layoutScopes = Array.Empty<FlowToolLayoutScopeItem>();
	// 当前激活节点。
	private HashSet<string> activeNodeIds = new(StringComparer.Ordinal);
	// 左侧作用域面板。
	private ScopePanel layoutScopePanel = null!;
	// 中央画布面板。
	private FlowToolCanvasGraphEdit canvasPanel = null!;
	// 右侧未分配池面板。
	private UnassignedPoolPanel unassignedPoolPanel = null!;

	public override void _Ready()
	{
		bindUi();
		bindSignals();
		reloadAndRender();
	}

	public override void _Process(double delta)
	{
		EmitSignal(SignalName.AutosavePulse, delta);
	}

	public override void _ExitTree()
	{
		EmitSignal(SignalName.AutosaveForced);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (tryGetCanvasLocalPointerPosition(out Vector2 canvasLocalPointerPosition) == false)
		{
			canvasPanel.ClearDropPreview();
			return false;
		}

		return canvasPanel.CanAcceptNodePayloadDropAtScreenPosition(canvasLocalPointerPosition, data);
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (tryGetCanvasLocalPointerPosition(out Vector2 canvasLocalPointerPosition) == false)
		{
			canvasPanel.ClearDropPreview();
			return;
		}

		canvasPanel.DropNodePayloadAtScreenPosition(canvasLocalPointerPosition, data);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationDragEnd)
		{
			canvasPanel.ClearDropPreview();
		}
	}

	// 绑定场景节点。
	private void bindUi()
	{
		layoutScopePanel = GetNode<ScopePanel>("SplitContainer/ScopePanel");
		canvasPanel = GetNode<FlowToolCanvasGraphEdit>("SplitContainer/ContentSplitContainer/EditorPanel/Canvas");
		unassignedPoolPanel = GetNode<UnassignedPoolPanel>("SplitContainer/ContentSplitContainer/UnassignedPoolBackground/UnassignedPoolPanel");
	}

	// 绑定组件信号。
	private void bindSignals()
	{
		canvasPanel.NodePayloadDropped += onNodePayloadDropped;
		canvasPanel.SetDeleteNodeRequested(onDeleteButtonPressed);
		layoutScopePanel.ScopeSelected += onLayoutScopeSelected;
	}

	// 计算当前鼠标在画布容器内的局部坐标。
	private bool tryGetCanvasLocalPointerPosition(out Vector2 canvasLocalPointerPosition)
	{
		canvasLocalPointerPosition = Vector2.Zero;
		Vector2 pointerGlobalPosition = GetGlobalMousePosition();
		Rect2 canvasGlobalRect = canvasPanel.GetGlobalRect();
		if (canvasGlobalRect.HasPoint(pointerGlobalPosition) == false)
		{
			return false;
		}

		canvasLocalPointerPosition = canvasPanel.GetGlobalTransformWithCanvas().AffineInverse() * pointerGlobalPosition;
		return true;
	}

	// 执行提取并重绘。
	private void reloadAndRender()
	{
		WorldCanvas fullTopology = topologyExtractor.ExtractFromCurrentAssembly();
		rebuildLayoutScopes(fullTopology);
		topology = selectedLayoutScopeKey == allLayoutScopeKey ? fullTopology : fullTopology.FilterByOwnerType(selectedLayoutScopeKey);
		IReadOnlyCollection<string> validNodeIds = topology.CollectMetricNodeIds();
		IReadOnlyDictionary<string, Vector2> persistedLayout = layoutStore.Load();
		layoutPositions = layoutStore.FilterInvalidNodes(persistedLayout, validNodeIds);
		activeNodeIds = topology.DeriveActiveNodeIds(layoutPositions.Keys);
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
		layoutStore = new FlowToolLayoutStore(selectedLayoutScopeKey);
		reloadAndRender();
	}

	// 构建布局作用域列表。
	private void rebuildLayoutScopes(WorldCanvas sourceTopology)
	{
		IReadOnlyList<FlowToolLayoutScopeItem> scoped = sourceTopology.BuildLayoutScopes();
		layoutScopes = (new[] { new FlowToolLayoutScopeItem(allLayoutScopeKey, "全部") })
			.Concat(scoped)
			.ToList();

		if (layoutScopes.Any(scope => scope.ScopeKey == selectedLayoutScopeKey) == false)
		{
			FlowToolLayoutScopeItem? fallbackScope = layoutScopes.FirstOrDefault();
			selectedLayoutScopeKey = fallbackScope?.ScopeKey ?? allLayoutScopeKey;
			layoutStore = new FlowToolLayoutStore(selectedLayoutScopeKey);
		}
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
