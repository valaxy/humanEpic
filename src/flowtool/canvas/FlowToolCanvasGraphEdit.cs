using Godot;
using GodotDictionary = Godot.Collections.Dictionary;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 左侧画布控制器，负责 2D 世界节点交互与双摄像机控制。
/// </summary>
[Tool]
[GlobalClass]
public partial class FlowToolCanvasGraphEdit : SubViewportContainer
{
	// 拖拽载荷中的节点 ID 键。
	private static readonly StringName dragNodeIdKey = "nodeId";

	// 节点默认宽度。
	private const float nodeWidth = 300f;
	// 节点默认高度。
	private const float nodeHeight = 110f;
	// 默认起始位置 X。
	private const float defaultPositionX = 80f;
	// 默认起始位置 Y。
	private const float defaultPositionY = 80f;
	// 画布最小缩放倍率。
	private const float minCanvasZoom = 0.55f;
	// 画布最大缩放倍率。
	private const float maxCanvasZoom = 2.1f;
	// 每次滚轮缩放步进。
	private const float canvasZoomStep = 0.1f;
	// 缩略图覆盖层边距。
	private const float minimapMargin = 12f;
	// 节点卡片场景资源。
	private static readonly PackedScene canvasNodeCardScene = GD.Load<PackedScene>("res://src/flowtool/canvas/flowtool_canvas_node_card.tscn");

	// 删除节点回调。
	private Action<string> deleteNodeRequested = static _ => { };
	// 画布领域对象（尺寸与约束规则）。
	private readonly FlowToolCanvasWorld canvasWorld = new(5000f, 3200f, nodeWidth, nodeHeight);
	// 当前节点数据映射。
	private Dictionary<string, FlowToolMetricNode> metricByNodeId = new(StringComparer.Ordinal);
	// 当前节点控件映射。
	private Dictionary<string, FlowToolCanvasNodeCard> cardByNodeId = new(StringComparer.Ordinal);
	// 当前节点布局映射。
	private Dictionary<string, Vector2> layoutByNodeId = new(StringComparer.Ordinal);
	// 当前边集合。
	private IReadOnlyList<FlowToolEdge> activeEdges = Array.Empty<FlowToolEdge>();
	// 当前选中节点 ID。
	private string selectedNodeId = string.Empty;
	// 当前拖拽节点 ID。
	private string draggingNodeId = string.Empty;
	// 主视口。
	private SubViewport mainViewport = null!;
	// 节点层。
	private Node2D nodesLayer = null!;
	// 世界渲染层。
	private FlowToolCanvasWorldLayer2D worldLayer = null!;
	// 主摄像机。
	private Camera2D mainCamera = null!;
	// 缩略图容器。
	private Control minimapPanel = null!;
	// 缩略图视口。
	private SubViewport minimapViewport = null!;
	// 缩略图摄像机。
	private Camera2D minimapCamera = null!;
	// 右下角缩略图覆盖层组件。
	private FlowToolCanvasMinimap minimap = null!;
	// 画布是否处于拖动平移状态。
	private bool isCanvasPanning;
	// 最近一次画布拖动时的鼠标位置。
	private Vector2 lastCanvasMousePosition = Vector2.Zero;

	/// <summary>
	/// 初始化画布内部依赖组件。
	/// </summary>
	public override void _Ready()
	{
		ClipContents = true;
		bindSceneNodes();
		configureViewports();
		configureMinimap();
		updateViewportSizes();
		worldLayer.ConfigureWorld(new Vector2(canvasWorld.Width, canvasWorld.Height), new Vector2(nodeWidth, nodeHeight));
		refreshMinimap();
	}

	/// <summary>
	/// 当前是否已有已渲染节点。
	/// </summary>
	public bool HasRenderedNodes => cardByNodeId.Count > 0;

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
	public void RenderTopology(FlowToolTopology topology, IReadOnlyCollection<string> activeNodeIds, IReadOnlyDictionary<string, Vector2> layoutPositions)
	{
		clearCards();
		metricByNodeId = topology.Metrics
			.Where(metric => activeNodeIds.Contains(metric.NodeId))
			.ToDictionary(static metric => metric.NodeId, static metric => metric, StringComparer.Ordinal);
		layoutByNodeId = metricByNodeId.Keys
			.ToDictionary(
				nodeId => nodeId,
				nodeId => layoutPositions.TryGetValue(nodeId, out Vector2 savedPosition) ? savedPosition : new Vector2(defaultPositionX, defaultPositionY),
				StringComparer.Ordinal);
		activeEdges = topology.Edges
			.Where(edge => activeNodeIds.Contains(edge.FromNodeId) && activeNodeIds.Contains(edge.ToNodeId))
			.ToList();

		metricByNodeId.Values
			.OrderBy(static metric => metric.DisplayName, StringComparer.Ordinal)
			.Select(createNodeCard)
			.ToList()
			.ForEach(card => nodesLayer.AddChild(card));

		worldLayer.UpdateGraph(layoutByNodeId, activeEdges);
		clampMainCameraPosition();

		UpdateDeleteButtonVisibility();
		refreshMinimap();
	}

	/// <summary>
	/// 采集当前画布布局坐标。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> CollectCurrentLayout()
	{
		return layoutByNodeId
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
	}

	/// <summary>
	/// 根据当前选中状态切换删除按钮可见性。
	/// </summary>
	public void UpdateDeleteButtonVisibility()
	{
		cardByNodeId
			.ToList()
			.ForEach(pair =>
			{
				pair.Value.SetDeleteVisible(pair.Key == selectedNodeId);
			});
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
			clearDropShadow();
			return false;
		}

		Vector2 shadowPosition = snapToCanvas(screenToWorld(localScreenPosition));
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
			clearDropShadow();
			return false;
		}

		Vector2 graphPosition = snapToCanvas(screenToWorld(localScreenPosition));
		clearDropShadow();
		EmitSignal(SignalName.NodePayloadDropped, nodeId, graphPosition);
		return true;
	}

	/// <summary>
	/// 清理外部拖拽产生的投影提示。
	/// </summary>
	public void ClearDropPreview()
	{
		clearDropShadow();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationDragEnd)
		{
			clearDropShadow();
			return;
		}

		if (what == NotificationResized)
		{
			updateViewportSizes();
			clampMainCameraPosition();
			refreshMinimap();
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
		{
			setCanvasZoom(mainCamera.Zoom.X - canvasZoomStep);
			return;
		}

		if (@event is InputEventMouseButton wheelDownButton && wheelDownButton.ButtonIndex == MouseButton.WheelDown && wheelDownButton.Pressed)
		{
			setCanvasZoom(mainCamera.Zoom.X + canvasZoomStep);
			return;
		}

		if (@event is InputEventMouseButton leftButton && leftButton.ButtonIndex == MouseButton.Left)
		{
			isCanvasPanning = leftButton.Pressed && string.IsNullOrWhiteSpace(draggingNodeId);
			lastCanvasMousePosition = leftButton.Position;
			return;
		}

		if (@event is InputEventMouseMotion motion && isCanvasPanning && string.IsNullOrWhiteSpace(draggingNodeId))
		{
			Vector2 delta = motion.Position - lastCanvasMousePosition;
			panCanvasBy(delta);
			lastCanvasMousePosition = motion.Position;
		}
	}

	// 绑定场景节点。
	private void bindSceneNodes()
	{
		mainViewport = GetNode<SubViewport>("MainViewport");
		nodesLayer = GetNode<Node2D>("MainViewport/CanvasRoot/NodesLayer");
		worldLayer = GetNode<FlowToolCanvasWorldLayer2D>("MainViewport/CanvasRoot/WorldLayer");
		mainCamera = GetNode<Camera2D>("MainViewport/CanvasRoot/MainCamera");
		minimapPanel = GetNode<Control>("Minimap");
		minimapViewport = GetNode<SubViewport>("Minimap/MinimapViewportContainer/MinimapViewport");
		minimapCamera = GetNode<Camera2D>("Minimap/MinimapViewportContainer/MinimapViewport/MinimapCamera");
		minimap = GetNode<FlowToolCanvasMinimap>("Minimap/MinimapOverlay");
	}

	// 配置主视口参数。
	private void configureViewports()
	{
		mainViewport.World2D ??= new World2D();
		mainViewport.HandleInputLocally = true;
		mainViewport.TransparentBg = false;
		mainViewport.Size2DOverrideStretch = false;
		mainCamera.MakeCurrent();
	}

	// 配置缩略图视口与摄像机。
	private void configureMinimap()
	{
		minimapViewport.World2D = mainViewport.World2D;
		minimapViewport.HandleInputLocally = false;
		minimapCamera.Enabled = true;
		minimapCamera.MakeCurrent();
		minimap.Configure(mainViewport, minimapViewport, mainCamera, minimapCamera, new Vector2(canvasWorld.Width, canvasWorld.Height), onMinimapNavigateRequested);
		fitMinimapCameraToWorld();
	}

	// 更新视口尺寸。
	private void updateViewportSizes()
	{
		Vector2I canvasViewportSize = new(
			Mathf.Max(Mathf.RoundToInt(Size.X), 1),
			Mathf.Max(Mathf.RoundToInt(Size.Y), 1));
		mainViewport.Size = canvasViewportSize;

		Vector2 minimapSize = calculateMinimapSize();
		minimapPanel.Size = minimapSize;
		minimapPanel.Position = new Vector2(Size.X - minimapSize.X - minimapMargin, Size.Y - minimapSize.Y - minimapMargin);
		Vector2I minimapViewportSize = new(
			Mathf.Max(Mathf.RoundToInt(minimapSize.X), 1),
			Mathf.Max(Mathf.RoundToInt(minimapSize.Y), 1));
		minimapViewport.Size = minimapViewportSize;
		fitMinimapCameraToWorld();
	}

	// 计算缩略图尺寸。
	private Vector2 calculateMinimapSize()
	{
		float width = Mathf.Clamp(Size.X * 0.24f, 180f, 300f);
		float height = Mathf.Clamp(Size.Y * 0.26f, 130f, 220f);
		return new Vector2(width, height);
	}

	// 清理当前所有节点控件。
	private void clearCards()
	{
		cardByNodeId.Values
			.ToList()
			.ForEach(card =>
			{
				nodesLayer.RemoveChild(card);
				card.QueueFree();
			});
		cardByNodeId = new Dictionary<string, FlowToolCanvasNodeCard>(StringComparer.Ordinal);
		selectedNodeId = string.Empty;
		draggingNodeId = string.Empty;
		worldLayer.UpdateGraph(layoutByNodeId, Array.Empty<FlowToolEdge>());
	}

	// 创建节点卡片控件。
	private FlowToolCanvasNodeCard createNodeCard(FlowToolMetricNode metricNode)
	{
		FlowToolCanvasNodeCard card = canvasNodeCardScene.Instantiate<FlowToolCanvasNodeCard>();
		card.Name = $"Card_{cardByNodeId.Count.ToString()}";
		card.Position = layoutByNodeId[metricNode.NodeId];
		card.Size = new Vector2(nodeWidth, nodeHeight);
		card.Configure(metricNode, deleteNodeRequested);
		card.SetSelected(isSelected: false);
		card.GuiInput += inputEvent => onCardGuiInput(metricNode.NodeId, inputEvent);
		cardByNodeId[metricNode.NodeId] = card;
		return card;
	}

	// 处理卡片鼠标交互。
	private void onCardGuiInput(string nodeId, InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex != MouseButton.Left)
			{
				return;
			}

			selectedNodeId = nodeId;
			if (mouseButton.Pressed)
			{
				isCanvasPanning = false;
				draggingNodeId = nodeId;
			}
			else
			{
				draggingNodeId = string.Empty;
			}

			UpdateDeleteButtonVisibility();
			updateCardSelectedStyle();
			QueueRedraw();
			return;
		}

		if (inputEvent is InputEventMouseMotion motion && string.IsNullOrWhiteSpace(draggingNodeId) == false && draggingNodeId == nodeId)
		{
			FlowToolCanvasNodeCard card = cardByNodeId[nodeId];
			Vector2 worldDelta = new(motion.Relative.X * mainCamera.Zoom.X, motion.Relative.Y * mainCamera.Zoom.Y);
			Vector2 nextPosition = card.Position + worldDelta;
			Vector2 snappedPosition = snapToCanvas(nextPosition);
			card.Position = snappedPosition;
			layoutByNodeId[nodeId] = snappedPosition;
			worldLayer.UpdateGraph(layoutByNodeId, activeEdges);
			refreshMinimap();
		}
	}

	// 更新选中节点的视觉样式。
	private void updateCardSelectedStyle()
	{
		cardByNodeId
			.ToList()
			.ForEach(pair =>
			{
				pair.Value.SetSelected(pair.Key == selectedNodeId);
			});
	}

	// 按增量平移主摄像机。
	private void panCanvasBy(Vector2 delta)
	{
		Vector2 worldDelta = new(delta.X * mainCamera.Zoom.X, delta.Y * mainCamera.Zoom.Y);
		mainCamera.Position = clampCameraPosition(mainCamera.Position - worldDelta);
		refreshMinimap();
	}

	// 调整画布缩放倍率。
	private void setCanvasZoom(float zoomValue)
	{
		float nextZoom = Mathf.Clamp(zoomValue, minCanvasZoom, maxCanvasZoom);
		mainCamera.Zoom = new Vector2(nextZoom, nextZoom);
		clampMainCameraPosition();
		refreshMinimap();
	}

	// 响应缩略图点击导航。
	private void onMinimapNavigateRequested(Vector2 targetWorldPosition)
	{
		mainCamera.Position = clampCameraPosition(targetWorldPosition);
		refreshMinimap();
	}

	// 刷新缩略图快照。
	private void refreshMinimap()
	{
		if (IsInsideTree() == false)
		{
			return;
		}

		minimap.Refresh();
	}

	// 清理拖拽影子节点。
	private void clearDropShadow()
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
		if (payload.TryGetValue(dragNodeIdKey, out Variant nodeIdValue) == false)
		{
			return false;
		}

		nodeId = nodeIdValue.AsString();
		return string.IsNullOrWhiteSpace(nodeId) == false;
	}

	// 约束节点坐标到画布范围。
	private Vector2 snapToCanvas(Vector2 position)
	{
		return canvasWorld.ClampNodePosition(position);
	}

	// 将屏幕坐标转换到主画布世界坐标。
	private Vector2 screenToWorld(Vector2 localScreenPosition)
	{
		Vector2 viewportSize = new(
			Mathf.Max(mainViewport.Size.X, 1),
			Mathf.Max(mainViewport.Size.Y, 1));
		Vector2 centered = localScreenPosition - (viewportSize / 2f);
		return new Vector2(
			mainCamera.Position.X + centered.X * mainCamera.Zoom.X,
			mainCamera.Position.Y + centered.Y * mainCamera.Zoom.Y);
	}

	// 将主摄像机限制在世界范围内。
	private void clampMainCameraPosition()
	{
		mainCamera.Position = clampCameraPosition(mainCamera.Position);
	}

	// 计算限制后的摄像机中心坐标。
	private Vector2 clampCameraPosition(Vector2 targetPosition)
	{
		Vector2 viewportSize = new(
			Mathf.Max(mainViewport.Size.X, 1),
			Mathf.Max(mainViewport.Size.Y, 1));
		Vector2 halfViewportInWorld = new(
			viewportSize.X * 0.5f * mainCamera.Zoom.X,
			viewportSize.Y * 0.5f * mainCamera.Zoom.Y);

		float minX = halfViewportInWorld.X;
		float maxX = canvasWorld.Width - halfViewportInWorld.X;
		float minY = halfViewportInWorld.Y;
		float maxY = canvasWorld.Height - halfViewportInWorld.Y;

		float safeX = minX > maxX
			? canvasWorld.Width * 0.5f
			: Mathf.Clamp(targetPosition.X, minX, maxX);
		float safeY = minY > maxY
			? canvasWorld.Height * 0.5f
			: Mathf.Clamp(targetPosition.Y, minY, maxY);
		return new Vector2(safeX, safeY);
	}

	// 让缩略图摄像机完整覆盖世界范围。
	private void fitMinimapCameraToWorld()
	{
		Vector2 minimapViewportSize = new(
			Mathf.Max(minimapViewport.Size.X, 1),
			Mathf.Max(minimapViewport.Size.Y, 1));
		float zoomX = canvasWorld.Width / minimapViewportSize.X;
		float zoomY = canvasWorld.Height / minimapViewportSize.Y;
		float fitZoom = Mathf.Max(Mathf.Max(zoomX, zoomY), 0.001f) * 1.03f;
		minimapCamera.Zoom = new Vector2(fitZoom, fitZoom);
		minimapCamera.Position = new Vector2(canvasWorld.Width * 0.5f, canvasWorld.Height * 0.5f);
	}
}
