using Godot;
using GodotDictionary = Godot.Collections.Dictionary;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 系统动力学仪表盘编辑器入口，负责作用域、画布渲染与交互。
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
	// 拖拽载荷中的节点 ID 键。
	private static readonly StringName dragNodeIdKey = "nodeId";
	// 节点默认宽度。
	private const float nodeWidth = 280f;
	// 节点默认高度。
	private const float nodeHeight = 96f;
	// 交互缩放控制器。
	private readonly CanvasZoomController zoomController = new();
	// 布局存储。
	private TopologyCanvasLayout layoutStore = new(allLayoutScopeKey);
	// 当前作用域拓扑。
	private GameSystem topology = GameSystem.Empty;
	// 画布当前渲染状态。
	private TopologyCanvas worldCanvas = new(5000f, 3200f, nodeWidth, nodeHeight);
	// 当前布局坐标。
	private IReadOnlyDictionary<string, Vector2> layoutPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
	// 当前布局作用域。
	private string selectedLayoutScopeKey = allLayoutScopeKey;
	// 当前布局作用域列表。
	private IReadOnlyList<Topology> layoutScopes = Array.Empty<Topology>();
	// 当前激活节点。
	private HashSet<string> activeNodeIds = new(StringComparer.Ordinal);
	// 当前选中节点。
	private string selectedNodeId = string.Empty;
	// 当前拖拽节点。
	private string draggingNodeId = string.Empty;
	// 拖拽偏移量。
	private Vector2 draggingPointerOffset = Vector2.Zero;
	// 左侧作用域面板。
	private ScopePanel layoutScopePanel = null!;
	// 右侧未分配池面板。
	private UnassignedPoolPanel unassignedPoolPanel = null!;
	// 世界层。
	private CanvasView worldLayer = null!;

	/// <summary>
	/// 当前是否已有已渲染节点。
	/// </summary>
	public bool HasRenderedNodes => worldCanvas.Nodes.Count > 0;

	/// <summary>
	/// 采集当前画布布局坐标。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> CollectCurrentLayout()
	{
		return worldCanvas.NodeLayout
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
	}

	public override void _Ready()
	{
		bindUi();
		bindSignals();
		worldLayer.SyncViewportSize();
		worldLayer.Initialize(worldCanvas);
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
			clearDropPreview();
			return false;
		}

		return canAcceptNodePayloadDropAtScreenPosition(canvasLocalPointerPosition, data);
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (tryGetCanvasLocalPointerPosition(out Vector2 canvasLocalPointerPosition) == false)
		{
			clearDropPreview();
			return;
		}

		dropNodePayloadAtScreenPosition(canvasLocalPointerPosition, data);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationDragEnd)
		{
			clearDropPreview();
			return;
		}

		if (what == NotificationResized)
		{
			worldLayer.SyncViewportSize();
		}
	}

	// 绑定场景节点。
	private void bindUi()
	{
		layoutScopePanel = GetNode<ScopePanel>("SplitContainer/ScopePanel");
		unassignedPoolPanel = GetNode<UnassignedPoolPanel>("SplitContainer/ContentSplitContainer/UnassignedPoolBackground/UnassignedPoolPanel");
		worldLayer = GetNode<CanvasView>("SplitContainer/ContentSplitContainer/EditorPanel/Canvas/MainViewport/CanvasRoot/WorldLayer");
	}

	// 绑定组件信号。
	private void bindSignals()
	{
		layoutScopePanel.ScopeSelected += onLayoutScopeSelected;
		worldLayer.NodeSelectedRecognized += handleNodeSelected;
		worldLayer.SelectedNodeDeleteRequested += onDeleteButtonPressed;
		worldLayer.MouseButtonInputRecognized += handleMouseButton;
		worldLayer.MouseMotionInputRecognized += handleMouseMotion;
	}

	// 计算当前鼠标在画布容器内的局部坐标。
	private bool tryGetCanvasLocalPointerPosition(out Vector2 canvasLocalPointerPosition)
	{
		Vector2 pointerGlobalPosition = GetGlobalMousePosition();
		return worldLayer.TryMapGlobalPointerToCanvas(pointerGlobalPosition, out canvasLocalPointerPosition);
	}

	// 处理鼠标按钮输入。
	private void handleMouseButton(InputEventMouseButton mouseButton)
	{
		if (worldLayer.TryHandleZoom(zoomController, mouseButton))
		{
			return;
		}

		if (mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		if (mouseButton.Pressed)
		{
			return;
		}

		draggingNodeId = string.Empty;
		draggingPointerOffset = Vector2.Zero;
	}

	// 处理节点点击选中输入。
	private void handleNodeSelected(string nodeId, Vector2 graphPointerPosition)
	{
		if (string.IsNullOrWhiteSpace(nodeId))
		{
			selectedNodeId = string.Empty;
			draggingNodeId = string.Empty;
			draggingPointerOffset = Vector2.Zero;
			updateDeleteButtonVisibility();
			return;
		}

		selectedNodeId = nodeId;
		draggingNodeId = nodeId;
		draggingPointerOffset = graphPointerPosition - worldCanvas.NodeLayout[nodeId];

		updateDeleteButtonVisibility();
	}

	// 处理鼠标拖拽输入。
	private void handleMouseMotion(InputEventMouseMotion mouseMotion)
	{
		if (string.IsNullOrWhiteSpace(draggingNodeId))
		{
			return;
		}

		if ((mouseMotion.ButtonMask & MouseButtonMask.Left) == 0)
		{
			return;
		}

		Vector2 nextPosition = mouseMotion.Position - draggingPointerOffset;
		Vector2 snappedPosition = snapToCanvas(nextPosition);
		Dictionary<string, Vector2> nextLayoutByNodeId = worldCanvas.NodeLayout
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		nextLayoutByNodeId[draggingNodeId] = snappedPosition;
		worldLayer.RenderGraph(worldCanvas.Nodes, nextLayoutByNodeId, worldCanvas.Edges, selectedNodeId);
	}

	// 执行提取并重绘。
	private void reloadAndRender()
	{
		GameSystem fullTopology = TopologyExtractor.ExtractFromCurrentAssembly();
		rebuildLayoutScopes(fullTopology);
		topology = selectedLayoutScopeKey == allLayoutScopeKey ? fullTopology : fullTopology.FilterByOwnerType(selectedLayoutScopeKey);
		IReadOnlyCollection<string> validNodeIds = topology.CollectMetricNodeIds();
		IReadOnlyDictionary<string, Vector2> persistedLayout = layoutStore.Load();
		layoutPositions = layoutStore.FilterInvalidNodes(persistedLayout, validNodeIds);
		activeNodeIds = topology.DeriveActiveNodeIds(layoutPositions.Keys);
		layoutScopePanel.Update(layoutScopes, selectedLayoutScopeKey);
		renderTopology(topology, activeNodeIds, layoutPositions);
		unassignedPoolPanel.Update(topology, activeNodeIds);
		EmitSignal(SignalName.AutosaveScopeChanged, selectedLayoutScopeKey);
		EmitSignal(SignalName.AutosaveCommitLayout);
	}

	// 渲染当前作用域下的节点与连线。
	private void renderTopology(GameSystem scopedTopology, IReadOnlyCollection<string> currentActiveNodeIds, IReadOnlyDictionary<string, Vector2> currentLayoutPositions)
	{
		clearCanvasState();
		(IReadOnlyDictionary<string, MetricNode> nodesByNodeId,
			IReadOnlyDictionary<string, Vector2> layoutByNodeId,
			IReadOnlyList<MetricEdge> currentEdges) = worldCanvas.BuildScopeSnapshot(
				scopedTopology,
				currentActiveNodeIds,
				currentLayoutPositions,
				new Vector2(80f, 80f));
		worldLayer.RenderGraph(nodesByNodeId, layoutByNodeId, currentEdges, selectedNodeId);
		updateDeleteButtonVisibility();
	}

	// 切换布局作用域。
	private void onLayoutScopeSelected(long selectedIndex)
	{
		if (selectedIndex < 0 || selectedIndex >= layoutScopes.Count)
		{
			return;
		}

		Topology selectedScope = layoutScopes[(int)selectedIndex];
		if (selectedScope.ScopeKey == selectedLayoutScopeKey)
		{
			return;
		}

		EmitSignal(SignalName.AutosaveSnapshotRequested);
		selectedLayoutScopeKey = selectedScope.ScopeKey;
		layoutStore = new TopologyCanvasLayout(selectedLayoutScopeKey);
		reloadAndRender();
	}

	// 构建布局作用域列表。
	private void rebuildLayoutScopes(GameSystem sourceTopology)
	{
		IReadOnlyList<Topology> scoped = sourceTopology.BuildLayoutScopes();
		layoutScopes = scoped.ToList();

		if (layoutScopes.Any(scope => scope.ScopeKey == selectedLayoutScopeKey) == false)
		{
			Topology? fallbackScope = layoutScopes.FirstOrDefault();
			selectedLayoutScopeKey = fallbackScope?.ScopeKey ?? allLayoutScopeKey;
			layoutStore = new TopologyCanvasLayout(selectedLayoutScopeKey);
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
				StringComparer.Ordinal);

		nextLayout[nodeId] = graphPosition;
		layoutPositions = nextLayout;

		renderTopology(topology, activeNodeIds, layoutPositions);
		unassignedPoolPanel.Update(topology, activeNodeIds);
		EmitSignal(SignalName.AutosaveForced);
	}

	// 删除节点并将其回收到未分配池。
	private void onDeleteButtonPressed(string nodeId)
	{
		activeNodeIds = activeNodeIds
			.Where(activeNodeId => activeNodeId != nodeId)
			.ToHashSet(StringComparer.Ordinal);
		layoutPositions = layoutPositions
			.Where(pair => activeNodeIds.Contains(pair.Key))
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		selectedNodeId = string.Empty;
		draggingNodeId = string.Empty;
		draggingPointerOffset = Vector2.Zero;

		renderTopology(topology, activeNodeIds, layoutPositions);
		unassignedPoolPanel.Update(topology, activeNodeIds);
		EmitSignal(SignalName.AutosaveForced);
	}

	// 根据当前选中状态同步节点高亮。
	private void updateDeleteButtonVisibility()
	{
		worldLayer.UpdateSelectedNode(selectedNodeId);
	}

	// 清理当前画布状态。
	private void clearCanvasState()
	{
		selectedNodeId = string.Empty;
		worldLayer.ClearGraph(selectedNodeId);
	}

	// 判断给定屏幕坐标处是否可接收节点拖拽，并更新投影提示。
	private bool canAcceptNodePayloadDropAtScreenPosition(Vector2 localScreenPosition, Variant data)
	{
		if (tryReadNodeIdFromDragData(data, out string nodeId) == false)
		{
			clearDropPreview();
			return false;
		}

		Vector2 shadowPosition = snapToCanvas(localScreenPosition);
		worldLayer.SetDropShadow(nodeId, shadowPosition);
		return true;
	}

	// 在给定屏幕坐标处执行节点落点。
	private bool dropNodePayloadAtScreenPosition(Vector2 localScreenPosition, Variant data)
	{
		if (tryReadNodeIdFromDragData(data, out string nodeId) == false)
		{
			clearDropPreview();
			return false;
		}

		Vector2 graphPosition = snapToCanvas(localScreenPosition);
		clearDropPreview();
		onNodePayloadDropped(nodeId, graphPosition);
		return true;
	}

	// 清理拖拽投影提示。
	private void clearDropPreview()
	{
		worldLayer.ClearDropShadow();
	}

	// 尝试从拖拽载荷中解析节点 ID。
	private static bool tryReadNodeIdFromDragData(Variant data, out string nodeId)
	{
		nodeId = string.Empty;
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}

		GodotDictionary payload = data.AsGodotDictionary();
		if (payload.TryGetValue(dragNodeIdKey, out Variant nodeIdValue) == false
			&& payload.TryGetValue("nodeId", out nodeIdValue) == false)
		{
			return false;
		}

		nodeId = nodeIdValue.AsString();
		return string.IsNullOrWhiteSpace(nodeId) == false;
	}

	// 约束节点坐标到画布范围。
	private Vector2 snapToCanvas(Vector2 position)
	{
		float safeX = Mathf.Clamp(position.X, 0f, worldCanvas.Width - worldCanvas.NodeWidth);
		float safeY = Mathf.Clamp(position.Y, 0f, worldCanvas.Height - worldCanvas.NodeHeight);
		return new Vector2(safeX, safeY);
	}
}
