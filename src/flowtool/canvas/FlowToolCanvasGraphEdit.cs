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
	// 节点基础背景色。
	private static readonly Color defaultNodeBackgroundColor = new(0.12f, 0.16f, 0.2f);
	// 节点选中背景色。
	private static readonly Color selectedNodeBackgroundColor = new(0.16f, 0.24f, 0.3f);
	// 节点边框色。
	private static readonly Color nodeBorderColor = new(0.39f, 0.69f, 0.92f);
	// 连线颜色。
	private static readonly Color edgeColor = new(0.46f, 0.74f, 0.95f);
	// 影子节点填充色。
	private static readonly Color dropShadowFillColor = new(0.78f, 0.86f, 0.98f, 0.18f);
	// 影子节点边框色。
	private static readonly Color dropShadowBorderColor = new(0.32f, 0.62f, 0.94f, 0.9f);

	// 删除节点回调。
	private Action<string> deleteNodeRequested = static _ => { };
	// 当前节点数据映射。
	private Dictionary<string, FlowToolMetricNode> metricByNodeId = new(StringComparer.Ordinal);
	// 当前节点控件映射。
	private Dictionary<string, PanelContainer> cardByNodeId = new(StringComparer.Ordinal);
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
				Button deleteButton = pair.Value.GetNode<Button>("Body/ActionRow/DeleteButton");
				deleteButton.Visible = pair.Key == selectedNodeId;
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
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		drawEdges();
		drawDropShadow();
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
		cardByNodeId = new Dictionary<string, PanelContainer>(StringComparer.Ordinal);
		selectedNodeId = string.Empty;
		draggingNodeId = string.Empty;
	}

	// 创建节点卡片控件。
	private PanelContainer createNodeCard(FlowToolMetricNode metricNode)
	{
		PanelContainer card = new()
		{
			Name = $"Card_{cardByNodeId.Count.ToString()}",
			Position = layoutByNodeId[metricNode.NodeId],
			Size = new Vector2(nodeWidth, nodeHeight),
			MouseFilter = MouseFilterEnum.Stop,
			FocusMode = FocusModeEnum.None
		};
		card.AddThemeStyleboxOverride("panel", createCardStyle(isSelected: false));

		VBoxContainer body = new()
		{
			Name = "Body",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore
		};
		body.SetAnchorsPreset(LayoutPreset.FullRect);
		body.OffsetLeft = 0f;
		body.OffsetTop = 0f;
		body.OffsetRight = 0f;
		body.OffsetBottom = 0f;
		HBoxContainer actionRow = new()
		{
			Name = "ActionRow",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Control actionSpacer = new()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		actionRow.AddChild(actionSpacer);
		Label titleLabel = new()
		{
			Text = metricNode.MetricName,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Label detailLabel = new()
		{
			Text = createNodeDetailText(metricNode),
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		Button deleteButton = new()
		{
			Name = "DeleteButton",
			Text = "删除",
			Visible = false,
			CustomMinimumSize = new Vector2(56f, 28f),
			MouseDefaultCursorShape = CursorShape.PointingHand,
			FocusMode = FocusModeEnum.None
		};
		deleteButton.Pressed += () => deleteNodeRequested(metricNode.NodeId);

		actionRow.AddChild(deleteButton);
		body.AddChild(actionRow);
		body.AddChild(titleLabel);
		body.AddChild(detailLabel);
		card.AddChild(body);

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
			PanelContainer card = cardByNodeId[nodeId];
			Vector2 nextPosition = card.Position + motion.Relative;
			Vector2 snappedPosition = snapToCanvas(nextPosition);
			card.Position = snappedPosition;
			layoutByNodeId[nodeId] = snappedPosition;
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
				pair.Value.AddThemeStyleboxOverride("panel", createCardStyle(pair.Key == selectedNodeId));
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
		PanelContainer fromCard = cardByNodeId[edge.FromNodeId];
		PanelContainer toCard = cardByNodeId[edge.ToNodeId];
		Vector2 fromCenter = fromCard.Position + (fromCard.Size / 2f);
		Vector2 toCenter = toCard.Position + (toCard.Size / 2f);

		if (edge.FromNodeId == edge.ToNodeId)
		{
			Vector2 start = fromCard.Position + new Vector2(fromCard.Size.X, fromCard.Size.Y * 0.5f);
			Vector2 turnRightUp = start + new Vector2(44f, -28f);
			Vector2 turnLeftUp = fromCard.Position + new Vector2(-44f, fromCard.Size.Y * 0.5f - 48f);
			Vector2 end = fromCard.Position + new Vector2(0f, fromCard.Size.Y * 0.5f);
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

	// 创建节点详情文本。
	private static string createNodeDetailText(FlowToolMetricNode metricNode)
	{
		string optionalDisplayLine = string.Equals(metricNode.DisplayName, metricNode.MetricName, StringComparison.Ordinal)
			? string.Empty
			: $"\n中文名: {metricNode.DisplayName}";
		return $"类型: {metricNode.TypeDisplayName}{optionalDisplayLine}";
	}

	// 创建节点卡片样式。
	private static StyleBoxFlat createCardStyle(bool isSelected)
	{
		return new StyleBoxFlat
		{
			BgColor = isSelected ? selectedNodeBackgroundColor : defaultNodeBackgroundColor,
			BorderColor = nodeBorderColor,
			BorderWidthBottom = 2,
			BorderWidthTop = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
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
		float safeX = Mathf.Clamp(position.X, 0f, Mathf.Max(Size.X - nodeWidth, 0f));
		float safeY = Mathf.Clamp(position.Y, 0f, Mathf.Max(Size.Y - nodeHeight, 0f));
		return new Vector2(Mathf.Round(safeX), Mathf.Round(safeY));
	}
}
