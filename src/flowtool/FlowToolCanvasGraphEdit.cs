using Godot;
using Godot.Collections;

/// <summary>
/// 左侧画布 GraphEdit，负责接受拖拽激活。
/// </summary>
public partial class FlowToolCanvasGraphEdit : GraphEdit
{
	// 过程节点类型标识。
	private const string processNodeKind = "process";
	// 影子节点标题前缀。
	private const string shadowTitlePrefix = "预放置";

	// 当前拖拽影子节点。
	private GraphNode? dropShadowNode;
	// 当前影子节点对应 ID。
	private string dropShadowNodeId = string.Empty;

	/// <summary>
	/// 节点拖入画布时触发。
	/// </summary>
	[Signal]
	public delegate void NodePayloadDroppedEventHandler(string nodeId, string nodeKind, Vector2 graphPosition);

	public override void _Notification(int what)
	{
		if (what == NotificationDragEnd)
		{
			clearDropShadow();
		}
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType != Variant.Type.Dictionary)
		{
			clearDropShadow();
			return false;
		}

		Dictionary payload = data.AsGodotDictionary();
		bool canDrop = payload.ContainsKey("nodeId") && payload.ContainsKey("nodeKind");
		if (canDrop == false)
		{
			clearDropShadow();
			return false;
		}

		string nodeId = payload["nodeId"].AsString();
		string nodeKind = payload["nodeKind"].AsString();
		string displayText = payload.ContainsKey("displayText") ? payload["displayText"].AsString() : nodeId;
		Vector2 graphPosition = toGraphPosition(atPosition);
		showOrMoveDropShadow(nodeId, nodeKind, displayText, graphPosition);
		return true;
	}

	public override bool _IsInInputHotzone(GodotObject inNode, int inPort, Vector2 mousePosition)
	{
		return false;
	}

	public override bool _IsInOutputHotzone(GodotObject inNode, int inPort, Vector2 mousePosition)
	{
		return false;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		Dictionary payload = data.AsGodotDictionary();
		string nodeId = payload["nodeId"].AsString();
		string nodeKind = payload["nodeKind"].AsString();
		Vector2 graphPosition = toGraphPosition(atPosition);
		clearDropShadow();
		EmitSignal(SignalName.NodePayloadDropped, nodeId, nodeKind, graphPosition);
	}

	// 将鼠标坐标转换为 GraphEdit 画布坐标，确保预览与落点一致。
	private Vector2 toGraphPosition(Vector2 atPosition)
	{
		float safeZoom = Mathf.IsZeroApprox(Zoom) ? 1f : Zoom;
		return (atPosition / safeZoom) + ScrollOffset;
	}

	// 显示或移动拖拽影子节点。
	private void showOrMoveDropShadow(string nodeId, string nodeKind, string displayText, Vector2 graphPosition)
	{
		if (dropShadowNode is null || dropShadowNodeId != nodeId)
		{
			clearDropShadow();
			dropShadowNode = createDropShadowNode(nodeId, nodeKind, displayText);
			dropShadowNodeId = nodeId;
			AddChild(dropShadowNode);
		}

		dropShadowNode.PositionOffset = graphPosition;
	}

	// 创建拖拽影子节点外观。
	private static GraphNode createDropShadowNode(string nodeId, string nodeKind, string displayText)
	{
		GraphNode shadowNode = new()
		{
			Name = $"DropShadow_{nodeId}",
			Title = $"{shadowTitlePrefix}: {displayText}",
			Draggable = false,
			Resizable = false,
			Selectable = false,
			Size = nodeKind == processNodeKind ? new Vector2(240f, 90f) : new Vector2(220f, 70f),
			MouseFilter = MouseFilterEnum.Ignore,
			Modulate = new Color(1f, 1f, 1f, 0.55f)
		};

		StyleBoxFlat panelStyle = new()
		{
			BgColor = new Color(0.78f, 0.86f, 0.98f, 0.22f),
			BorderColor = new Color(0.32f, 0.62f, 0.94f, 0.9f),
			BorderWidthTop = 2,
			BorderWidthBottom = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
		shadowNode.AddThemeStyleboxOverride("panel", panelStyle);

		Label hintLabel = new()
		{
			Text = "松开鼠标后将在此放置",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		shadowNode.AddChild(hintLabel);

		return shadowNode;
	}

	// 清理拖拽影子节点。
	private void clearDropShadow()
	{
		dropShadowNode?.QueueFree();
		dropShadowNode = null;
		dropShadowNodeId = string.Empty;
	}
}
