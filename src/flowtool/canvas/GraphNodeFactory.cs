using Godot;
using System;

/// <summary>
/// GraphNode 创建工厂。
/// </summary>
public static class GraphNodeFactory
{
	// 影子节点标题前缀。
	private const string shadowTitlePrefix = "预放置";

	/// <summary>
	/// 创建拖拽影子节点外观。
	/// </summary>
	public static GraphNode CreateDropShadowNode(string nodeId, string displayText)
	{
		GraphNode shadowNode = new()
		{
			Name = $"DropShadow_{nodeId}",
			Title = $"{shadowTitlePrefix}: {displayText}",
			Draggable = false,
			Resizable = false,
			Selectable = false,
			Size = new Vector2(240f, 80f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
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
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		shadowNode.AddChild(hintLabel);

		return shadowNode;
	}

	/// <summary>
	/// 创建指标 GraphNode 与其删除按钮。
	/// </summary>
	public static FlowToolGraphNodeBuildResult CreateMetricNode(
		string nodeName,
		string nodeId,
		string displayName,
		string detailText,
		Vector2 positionOffset,
		Action onDeleteRequested)
	{
		GraphNode graphNode = new()
		{
			Name = nodeName,
			Title = string.Empty,
			PositionOffset = positionOffset,
			Resizable = false,
			Draggable = true,
			CustomMinimumSize = new Vector2(240f, 0f)
		};
		graphNode.GetTitlebarHBox().Visible = false;

		VBoxContainer body = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		HBoxContainer header = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		Label titleLabel = new()
		{
			Text = displayName,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		Button deleteButton = createDeleteButton(onDeleteRequested);
		Label detailLabel = new()
		{
			Text = detailText,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		titleLabel.SizeFlagsStretchRatio = 1f;
		header.AddChild(titleLabel);
		header.AddChild(deleteButton);
		body.AddChild(header);
		body.AddChild(detailLabel);
		graphNode.AddChild(body);

		Color portColor = new Color(0.39f, 0.69f, 0.92f);
		graphNode.SetSlot(0, true, 0, portColor, true, 0, portColor);
		
		applyMetricNodeStyle(graphNode);
		graphNode.ResetSize();
		
		Vector2 compactSize = graphNode.Size;
		graphNode.Size = new Vector2(Mathf.Max(compactSize.X, 240f), Mathf.Clamp(compactSize.Y, 72f, 120f));

		return new FlowToolGraphNodeBuildResult(nodeId, graphNode, deleteButton);
	}

	// 创建仅在选中时显示的删除按钮。
	private static Button createDeleteButton(Action onDeleteRequested)
	{
		Button deleteButton = new()
		{
			Text = "删除",
			Visible = false,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		deleteButton.Pressed += onDeleteRequested;
		return deleteButton;
	}

	// 设置指标节点外观。
	private static void applyMetricNodeStyle(GraphNode graphNode)
	{
		StyleBoxFlat panelStyle = new()
		{
			BgColor = new Color(0.12f, 0.16f, 0.2f),
			BorderColor = new Color(0.39f, 0.69f, 0.92f),
			BorderWidthBottom = 2,
			BorderWidthTop = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
		graphNode.AddThemeStyleboxOverride("panel", panelStyle);
	}
}

/// <summary>
/// GraphNode 工厂创建结果。
/// </summary>
public sealed record FlowToolGraphNodeBuildResult(string NodeId, GraphNode GraphNode, Button DeleteButton);
