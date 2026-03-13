using Godot;
using GodotDictionary = Godot.Collections.Dictionary;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 左侧画布控制器，负责 2D 世界节点交互与双摄像机控制。
/// </summary>
[GlobalClass]
public partial class FlowToolCanvasGraphEdit : SubViewportContainer
{
	// 拖拽载荷中的节点 ID 键。
	private static readonly StringName dragNodeIdKey = "nodeId";
	// 节点默认宽度。
	private const float nodeWidth = 280f;
	// 节点默认高度。
	private const float nodeHeight = 96f;
	// 删除节点回调。
	private Action<string> deleteNodeRequested = static _ => { };
	// 画布领域模型。
	private WorldCanvas worldCanvas = new(5000f, 3200f, nodeWidth, nodeHeight);
	// 当前选中节点。
	private string selectedNodeId = string.Empty;
	// 主视口。
	private SubViewport mainViewport = null!;
	// 世界层。
	private WorldCanvasView worldLayer = null!;
	// 主摄像机。
	private Camera2D mainCamera = null!;

	/// <summary>
	/// 初始化画布内部依赖组件。
	/// </summary>
	public override void _Ready()
	{
		ClipContents = true;
		bindSceneNodes();
		bindWorldLayerSignals();
		configureMainViewport();
		updateViewportSizes();
		worldLayer.Initialize(worldCanvas);
	}

	/// <summary>
	/// 当前是否已有已渲染节点。
	/// </summary>
	public bool HasRenderedNodes => worldCanvas.Nodes.Count > 0;

	/// <summary>
	/// 节点拖入画布时触发。
	/// </summary>
	[Signal]
	public delegate void NodePayloadDroppedEventHandler(string nodeId, Vector2 graphPosition);

	/// <summary>
	/// 绑定删除节点回调。
	/// </summary>
	public void SetDeleteNodeRequested(Action<string> deleteNodeRequested)
	{
		this.deleteNodeRequested = deleteNodeRequested;
	}

	/// <summary>
	/// 渲染当前作用域下的节点与连线。
	/// </summary>
	public void RenderTopology(WorldCanvas topology, IReadOnlyCollection<string> activeNodeIds, IReadOnlyDictionary<string, Vector2> layoutPositions)
	{
		clearCanvasState();
		Dictionary<string, MetricNode> nodesByNodeId = topology.Metrics
			.Where(metric => activeNodeIds.Contains(metric.NodeId))
			.ToDictionary(metric => metric.NodeId, metric => metric, StringComparer.Ordinal);

		Dictionary<string, Vector2> layoutByNodeId = activeNodeIds
			.ToDictionary(
				nodeId => nodeId,
				nodeId => layoutPositions.TryGetValue(nodeId, out Vector2 savedPosition) ? savedPosition : new Vector2(80f, 80f),
				StringComparer.Ordinal);

		IReadOnlyList<MetricEdge> currentEdges = topology.Edges
			.Where(edge => activeNodeIds.Contains(edge.FromNodeId) && activeNodeIds.Contains(edge.ToNodeId))
			.ToList();
		worldCanvas.UpdateGraph(nodesByNodeId, layoutByNodeId, currentEdges);
		worldLayer.QueueRedraw();
		UpdateDeleteButtonVisibility();
	}

	/// <summary>
	/// 采集当前画布布局坐标。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> CollectCurrentLayout()
	{
		return worldCanvas.NodeLayout
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
	}

	/// <summary>
	/// 根据当前选中状态切换删除按钮可见性。
	/// </summary>
	public void UpdateDeleteButtonVisibility()
	{
		worldLayer.SetSelectedNode(selectedNodeId);
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		return CanAcceptNodePayloadDropAtScreenPosition(atPosition, data);
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		DropNodePayloadAtScreenPosition(atPosition, data);
	}

	/// <summary>
	/// 判断给定屏幕坐标处是否可接收节点拖拽，并更新投影提示。
	/// </summary>
	public bool CanAcceptNodePayloadDropAtScreenPosition(Vector2 localScreenPosition, Variant data)
	{
		if (tryReadNodeIdFromDragData(data, out string nodeId) == false)
		{
			worldLayer.ClearDropShadow();
			return false;
		}

		Vector2 shadowPosition = snapToCanvas(localScreenPosition);
		worldLayer.SetDropShadow(nodeId, shadowPosition);
		return true;
	}

	/// <summary>
	/// 在给定屏幕坐标处执行节点落点。
	/// </summary>
	public bool DropNodePayloadAtScreenPosition(Vector2 localScreenPosition, Variant data)
	{
		if (tryReadNodeIdFromDragData(data, out string nodeId) == false)
		{
			worldLayer.ClearDropShadow();
			return false;
		}

		Vector2 graphPosition = snapToCanvas(localScreenPosition);
		worldLayer.ClearDropShadow();
		worldLayer.NotifyNodePayloadDropped(nodeId, graphPosition);
		return true;
	}

	/// <summary>
	/// 清理外部拖拽产生的投影提示。
	/// </summary>
	public void ClearDropPreview()
	{
		worldLayer.ClearDropShadow();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationDragEnd)
		{
			worldLayer.ClearDropShadow();
			return;
		}

		if (what == NotificationResized)
		{
			updateViewportSizes();
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		worldLayer.HandleInputEvent(@event, mainCamera);
	}

	// 绑定场景节点。
	private void bindSceneNodes()
	{
		mainViewport = GetNode<SubViewport>("MainViewport");
		worldLayer = GetNode<WorldCanvasView>("MainViewport/CanvasRoot/WorldLayer");
		mainCamera = GetNode<Camera2D>("MainViewport/CanvasRoot/MainCamera");
	}

	// 绑定世界层基础信号。
	private void bindWorldLayerSignals()
	{
		worldLayer.NodeSelected += onWorldLayerNodeSelected;
		worldLayer.NodeDragged += onWorldLayerNodeDragged;
		worldLayer.DeleteRequested += onWorldLayerDeleteRequested;
		worldLayer.NodePayloadDropped += onWorldLayerNodePayloadDropped;
	}

	// 配置主视口。
	private void configureMainViewport()
	{
		mainViewport.World2D ??= new World2D();
		mainViewport.HandleInputLocally = true;
		mainViewport.TransparentBg = false;
		mainViewport.Size2DOverrideStretch = false;
		mainCamera.Enabled = true;
		mainCamera.MakeCurrent();
	}

	// 更新视口尺寸。
	private void updateViewportSizes()
	{
		Vector2I canvasViewportSize = new(
			Mathf.Max(Mathf.RoundToInt(Size.X), 1),
			Mathf.Max(Mathf.RoundToInt(Size.Y), 1));
		mainViewport.Size = canvasViewportSize;
	}

	// 清理当前画布状态。
	private void clearCanvasState()
	{
		selectedNodeId = string.Empty;
		worldCanvas.UpdateGraph(
			new Dictionary<string, MetricNode>(StringComparer.Ordinal),
			new Dictionary<string, Vector2>(StringComparer.Ordinal),
			Array.Empty<MetricEdge>());
		worldLayer.SetSelectedNode(selectedNodeId);
		worldLayer.QueueRedraw();
	}

	// 处理世界层选中信号。
	private void onWorldLayerNodeSelected(string nodeId)
	{
		selectedNodeId = nodeId;
		UpdateDeleteButtonVisibility();
	}

	// 处理世界层拖拽信号。
	private void onWorldLayerNodeDragged(string nodeId, Vector2 position)
	{
		Dictionary<string, Vector2> nextLayoutByNodeId = worldCanvas.NodeLayout
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		nextLayoutByNodeId[nodeId] = position;
		worldCanvas.UpdateGraph(worldCanvas.Nodes, nextLayoutByNodeId, worldCanvas.Edges);
		worldLayer.QueueRedraw();
	}

	// 处理世界层删除请求信号。
	private void onWorldLayerDeleteRequested(string nodeId)
	{
		deleteNodeRequested(nodeId);
	}

	// 处理世界层节点落点信号。
	private void onWorldLayerNodePayloadDropped(string nodeId, Vector2 graphPosition)
	{
		EmitSignal(SignalName.NodePayloadDropped, nodeId, graphPosition);
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
