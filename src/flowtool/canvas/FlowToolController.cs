using Godot;
using GodotDictionary = Godot.Collections.Dictionary;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// flowtool 总控制器，负责协调 Domain 与 View。
/// </summary>
[GlobalClass]
public partial class FlowToolController : Control
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

	// 拖拽载荷中的节点 ID 键。
	private static readonly StringName dragNodeIdKey = "nodeId";
	// 布局持久化器。
	private readonly TopologyCanvasLayout layoutStore = new();
	// 缩放控制器。
	private readonly CanvasZoomController zoomController = new();
	// 画布领域单例。
	private readonly TopologyCanvas topologyCanvas = TopologyCanvas.Instance;
	// 当前系统拓扑。
	private GameSystem gameSystem = GameSystem.Empty;
	// 当前作用域列表。
	private IReadOnlyList<Topology> layoutScopes = Array.Empty<Topology>();
	// 当前选中作用域键。
	private string selectedLayoutScopeKey = GameSystem.AllTopologyScopeKey;
	// 当前拖拽节点 ID。
	private string draggingNodeId = string.Empty;
	// 当前拖拽偏移。
	private Vector2 draggingPointerOffset = Vector2.Zero;
	// 左侧作用域面板。
	private ScopePanel scopePanel = null!;
	// 右侧未分配池。
	private UnassignedPoolPanel unassignedPoolPanel = null!;
	// 中央画布视图。
	private CanvasView canvasView = null!;

	/// <summary>
	/// 当前是否已渲染节点。
	/// </summary>
	public bool HasRenderedNodes => topologyCanvas.NodesByNodeId.Count > 0;

	/// <summary>
	/// 读取当前布局快照。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> CollectCurrentLayout()
	{
		return topologyCanvas.LayoutPositions
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
	}

	public override void _Ready()
	{
		bindUi();
		bindSignals();
		canvasView.SyncViewportSize();
		canvasView.Initialize(topologyCanvas);
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
			canvasView.SyncViewportSize();
		}
	}

	// 绑定场景节点。
	private void bindUi()
	{
		scopePanel = GetNode<ScopePanel>("SplitContainer/ScopePanel");
		unassignedPoolPanel = GetNode<UnassignedPoolPanel>("SplitContainer/ContentSplitContainer/UnassignedPoolBackground/UnassignedPoolPanel");
		canvasView = GetNode<CanvasView>("SplitContainer/ContentSplitContainer/EditorPanel/Canvas/MainViewport/CanvasRoot/WorldLayer");
	}

	// 绑定组件信号。
	private void bindSignals()
	{
		scopePanel.ScopeSelected += onLayoutScopeSelected;
		canvasView.NodeSelectedRecognized += onNodeSelected;
		canvasView.SelectedNodeDeleteRequested += onDeleteButtonPressed;
		canvasView.MouseButtonInputRecognized += onMouseButtonInput;
		canvasView.MouseMotionInputRecognized += onMouseMotionInput;
	}

	// 重新提取并渲染当前作用域。
	private void reloadAndRender()
	{
		gameSystem = ClassInfoExtractor.ExtractFromCurrentAssembly();
		rebuildLayoutScopes();
		IReadOnlyDictionary<string, Vector2> persistedLayout = layoutStore.Load(selectedLayoutScopeKey);
		topologyCanvas.Reload(gameSystem, selectedLayoutScopeKey, persistedLayout);
		scopePanel.Update(layoutScopes, selectedLayoutScopeKey);
		renderCanvas();
		unassignedPoolPanel.Update(topologyCanvas.CurrentTopology, topologyCanvas.ActiveNodeIds);
		EmitSignal(SignalName.AutosaveScopeChanged, selectedLayoutScopeKey);
		EmitSignal(SignalName.AutosaveCommitLayout);
	}

	// 重绘画布快照。
	private void renderCanvas()
	{
		canvasView.RenderGraph(
			topologyCanvas.NodesByNodeId,
			topologyCanvas.NodeLayoutByNodeId,
			topologyCanvas.ActiveEdges,
			topologyCanvas.SelectedNodeId);
	}

	// 切换作用域。
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
		reloadAndRender();
	}

	// 重建作用域列表并保证当前选择有效。
	private void rebuildLayoutScopes()
	{
		layoutScopes = gameSystem.BuildLayoutScopes();
		if (layoutScopes.Any(scope => scope.ScopeKey == selectedLayoutScopeKey))
		{
			return;
		}

		Topology? fallbackScope = layoutScopes.FirstOrDefault();
		selectedLayoutScopeKey = fallbackScope?.ScopeKey ?? GameSystem.AllTopologyScopeKey;
	}

	// 处理节点选中。
	private void onNodeSelected(string nodeId, Vector2 graphPointerPosition)
	{
		if (string.IsNullOrWhiteSpace(nodeId))
		{
			topologyCanvas.SelectNode(string.Empty);
			draggingNodeId = string.Empty;
			draggingPointerOffset = Vector2.Zero;
			renderCanvas();
			return;
		}

		topologyCanvas.SelectNode(nodeId);
		draggingNodeId = nodeId;
		draggingPointerOffset = graphPointerPosition - topologyCanvas.NodeLayoutByNodeId[nodeId];
		renderCanvas();
	}

	// 处理鼠标按钮输入。
	private void onMouseButtonInput(InputEventMouseButton mouseButton)
	{
		if (canvasView.TryHandleZoom(zoomController, mouseButton))
		{
			return;
		}

		if (mouseButton.ButtonIndex != MouseButton.Left || mouseButton.Pressed)
		{
			return;
		}

		draggingNodeId = string.Empty;
		draggingPointerOffset = Vector2.Zero;
	}

	// 处理鼠标拖拽输入。
	private void onMouseMotionInput(InputEventMouseMotion mouseMotion)
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
		if (topologyCanvas.TryUpdateNodeLayout(draggingNodeId, snappedPosition) == false)
		{
			return;
		}

		renderCanvas();
	}

	// 删除节点并刷新未分配池。
	private void onDeleteButtonPressed(string nodeId)
	{
		if (topologyCanvas.RemoveNode(nodeId) == false)
		{
			return;
		}

		draggingNodeId = string.Empty;
		draggingPointerOffset = Vector2.Zero;
		renderCanvas();
		unassignedPoolPanel.Update(topologyCanvas.CurrentTopology, topologyCanvas.ActiveNodeIds);
		EmitSignal(SignalName.AutosaveForced);
	}

	// 计算当前鼠标在画布容器内的局部坐标。
	private bool tryGetCanvasLocalPointerPosition(out Vector2 canvasLocalPointerPosition)
	{
		Vector2 pointerGlobalPosition = GetGlobalMousePosition();
		return canvasView.TryMapGlobalPointerToCanvas(pointerGlobalPosition, out canvasLocalPointerPosition);
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
		canvasView.SetDropShadow(nodeId, shadowPosition);
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
		if (topologyCanvas.ActivateNode(nodeId, graphPosition) == false)
		{
			return false;
		}

		renderCanvas();
		unassignedPoolPanel.Update(topologyCanvas.CurrentTopology, topologyCanvas.ActiveNodeIds);
		EmitSignal(SignalName.AutosaveForced);
		return true;
	}

	// 清理拖拽投影提示。
	private void clearDropPreview()
	{
		canvasView.ClearDropShadow();
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

	// 将节点坐标钳制在画布范围内。
	private Vector2 snapToCanvas(Vector2 position)
	{
		float safeX = Mathf.Clamp(position.X, 0f, topologyCanvas.Width - topologyCanvas.NodeWidth);
		float safeY = Mathf.Clamp(position.Y, 0f, topologyCanvas.Height - topologyCanvas.NodeHeight);
		return new Vector2(safeX, safeY);
	}
}
