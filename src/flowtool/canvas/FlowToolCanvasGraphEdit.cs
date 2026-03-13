using Godot;
using GodotDictionary = Godot.Collections.Dictionary;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 左侧画布 Control，负责自绘连线与节点拖拽交互。
/// </summary>
public partial class FlowToolCanvasGraphEdit : Control
{
	// 节点默认宽度。
	private const float nodeWidth = 300f;
	// 节点默认高度。
	private const float nodeHeight = 110f;
	// 默认起始位置 X。
	private const float defaultPositionX = 80f;
	// 默认起始位置 Y。
	private const float defaultPositionY = 80f;
	// 节点卡片场景资源。
	private static readonly PackedScene canvasNodeCardScene = GD.Load<PackedScene>("res://src/flowtool/canvas/flowtool_canvas_node_card.tscn");
	// 画布最小缩放倍率。
	private const float minCanvasZoom = 0.6f;
	// 画布最大缩放倍率。
	private const float maxCanvasZoom = 1.8f;
	// 每次滚轮缩放步进。
	private const float canvasZoomStep = 0.1f;
	// 到达边界时背景提示条宽度。
	private const float boundaryHintBandSize = 24f;
	// 连线颜色。
	private static readonly Color edgeColor = new(0.46f, 0.74f, 0.95f);
	// 视口背景色。
	private static readonly Color viewportBackgroundColor = new(0.06f, 0.08f, 0.11f);
	// 画布背景色。
	private static readonly Color canvasBackgroundColor = new(0.11f, 0.14f, 0.18f);
	// 影子节点填充色。
	private static readonly Color dropShadowFillColor = new(0.78f, 0.86f, 0.98f, 0.18f);
	// 影子节点边框色。
	private static readonly Color dropShadowBorderColor = new(0.32f, 0.62f, 0.94f, 0.9f);

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
	// 当前影子节点位置。
	private Vector2 dropShadowPosition = Vector2.Zero;
	// 当前影子节点 ID。
	private string dropShadowNodeId = string.Empty;
	// 右下角缩略图组件。
	private FlowToolCanvasMinimap minimap = null!;
	// 当前画布缩放倍率。
	private float canvasZoom = 1f;
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
		minimap = GetNode<FlowToolCanvasMinimap>("Minimap");
		minimap.SetNavigateRequested(onMinimapNavigateRequested);
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
			.ForEach(card => AddChild(card));

		UpdateDeleteButtonVisibility();
		refreshMinimap();
		QueueRedraw();
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
		if (data.VariantType != Variant.Type.Dictionary)
		{
			clearDropShadow();
			return false;
		}

		GodotDictionary payload = data.AsGodotDictionary();
		if (payload.ContainsKey("nodeId") == false)
		{
			clearDropShadow();
			return false;
		}

		dropShadowNodeId = payload["nodeId"].AsString();
		dropShadowPosition = snapToCanvas(atPosition);
		QueueRedraw();
		return true;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		GodotDictionary payload = data.AsGodotDictionary();
		string nodeId = payload["nodeId"].AsString();
		Vector2 graphPosition = snapToCanvas(atPosition);
		clearDropShadow();
		EmitSignal(SignalName.NodePayloadDropped, nodeId, graphPosition);
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
			refreshMinimap();
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		drawCanvasBackground();
		drawEdges();
		drawDropShadow();
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
		{
			setCanvasZoom(canvasZoom + canvasZoomStep);
			return;
		}

		if (@event is InputEventMouseButton wheelDownButton && wheelDownButton.ButtonIndex == MouseButton.WheelDown && wheelDownButton.Pressed)
		{
			setCanvasZoom(canvasZoom - canvasZoomStep);
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

	// 清理当前所有节点控件。
	private void clearCards()
	{
		cardByNodeId.Values
			.ToList()
			.ForEach(card =>
			{
				RemoveChild(card);
				card.QueueFree();
			});
		cardByNodeId = new Dictionary<string, FlowToolCanvasNodeCard>(StringComparer.Ordinal);
		selectedNodeId = string.Empty;
		draggingNodeId = string.Empty;
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
			Vector2 nextPosition = card.Position + motion.Relative;
			Vector2 snappedPosition = snapToCanvas(nextPosition);
			card.Position = snappedPosition;
			layoutByNodeId[nodeId] = snappedPosition;
			refreshMinimap();
			QueueRedraw();
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

	// 绘制所有连线。
	private void drawEdges()
	{
		activeEdges
			.Where(edge => cardByNodeId.ContainsKey(edge.FromNodeId) && cardByNodeId.ContainsKey(edge.ToNodeId))
			.ToList()
			.ForEach(drawEdge);
	}

	// 绘制单条连线（支持自环）。
	private void drawEdge(FlowToolEdge edge)
	{
		FlowToolCanvasNodeCard fromCard = cardByNodeId[edge.FromNodeId];
		FlowToolCanvasNodeCard toCard = cardByNodeId[edge.ToNodeId];
		Vector2 cardSize = new(nodeWidth, nodeHeight);
		Vector2 fromCenter = fromCard.Position + (cardSize / 2f);
		Vector2 toCenter = toCard.Position + (cardSize / 2f);

		if (edge.FromNodeId == edge.ToNodeId)
		{
			Vector2 start = fromCard.Position + new Vector2(cardSize.X, cardSize.Y * 0.5f);
			Vector2 turnRightUp = start + new Vector2(44f, -28f);
			Vector2 turnLeftUp = fromCard.Position + new Vector2(-44f, cardSize.Y * 0.5f - 48f);
			Vector2 end = fromCard.Position + new Vector2(0f, cardSize.Y * 0.5f);
			DrawLine(start, turnRightUp, edgeColor, 2f, true);
			DrawLine(turnRightUp, turnLeftUp, edgeColor, 2f, true);
			DrawLine(turnLeftUp, end, edgeColor, 2f, true);
			return;
		}

		DrawLine(fromCenter, toCenter, edgeColor, 2f, true);
	}

	// 绘制拖拽影子节点。
	private void drawDropShadow()
	{
		if (string.IsNullOrWhiteSpace(dropShadowNodeId))
		{
			return;
		}

		Rect2 shadowRect = new(dropShadowPosition, new Vector2(nodeWidth, nodeHeight));
		DrawRect(shadowRect, dropShadowFillColor, true);
		DrawRect(shadowRect, dropShadowBorderColor, false, 2f);
	}

	// 绘制视口背景与画布区域背景。
	private void drawCanvasBackground()
	{
		DrawRect(new Rect2(Vector2.Zero, Size), viewportBackgroundColor, true);
		DrawRect(createCanvasVisibleRect(), canvasBackgroundColor, true);
	}

	// 生成当前可见画布区域矩形，到达边界时露出背景提示条。
	private Rect2 createCanvasVisibleRect()
	{
		float leftInset = layoutByNodeId.Values.Any(position => position.X <= 0f) ? boundaryHintBandSize : 0f;
		float topInset = layoutByNodeId.Values.Any(position => position.Y <= 0f) ? boundaryHintBandSize : 0f;
		float rightBoundary = canvasWorld.Width - nodeWidth;
		float bottomBoundary = canvasWorld.Height - nodeHeight;
		float rightInset = layoutByNodeId.Values.Any(position => position.X >= rightBoundary) ? boundaryHintBandSize : 0f;
		float bottomInset = layoutByNodeId.Values.Any(position => position.Y >= bottomBoundary) ? boundaryHintBandSize : 0f;

		float x = leftInset;
		float y = topInset;
		float width = Mathf.Max(Size.X - leftInset - rightInset, 1f);
		float height = Mathf.Max(Size.Y - topInset - bottomInset, 1f);
		return new Rect2(new Vector2(x, y), new Vector2(width, height));
	}

	// 按增量平移整个画布中的节点布局。
	private void panCanvasBy(Vector2 delta)
	{
		cardByNodeId
			.ToList()
			.ForEach(pair =>
			{
				Vector2 nextPosition = canvasWorld.TranslateWithConstraint(pair.Value.Position, delta);
				pair.Value.Position = nextPosition;
				layoutByNodeId[pair.Key] = nextPosition;
			});
		refreshMinimap();
		QueueRedraw();
	}

	// 调整画布缩放倍率。
	private void setCanvasZoom(float zoomValue)
	{
		canvasZoom = Mathf.Clamp(zoomValue, minCanvasZoom, maxCanvasZoom);
		Scale = new Vector2(canvasZoom, canvasZoom);
		refreshMinimap();
		QueueRedraw();
	}

	// 响应缩略图点击导航。
	private void onMinimapNavigateRequested(Vector2 targetWorldPosition)
	{
		Vector2 viewportCenter = new(Size.X * 0.5f, Size.Y * 0.5f);
		Vector2 delta = viewportCenter - targetWorldPosition;
		panCanvasBy(delta);
	}

	// 刷新缩略图快照。
	private void refreshMinimap()
	{
		if (IsInsideTree() == false)
		{
			return;
		}

		minimap.UpdateSnapshot(layoutByNodeId, Size, new Vector2(canvasWorld.Width, canvasWorld.Height));
	}

	// 清理拖拽影子节点。
	private void clearDropShadow()
	{
		dropShadowNodeId = string.Empty;
		dropShadowPosition = Vector2.Zero;
		QueueRedraw();
	}

	// 约束节点坐标到画布范围。
	private Vector2 snapToCanvas(Vector2 position)
	{
		return canvasWorld.ClampNodePosition(position);
	}
}
