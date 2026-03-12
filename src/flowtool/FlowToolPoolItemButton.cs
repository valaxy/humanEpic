using Godot;
using Godot.Collections;

/// <summary>
/// 右侧未分配池中的可拖拽节点按钮。
/// </summary>
public partial class FlowToolPoolItemButton : Button
{
	// 节点 ID。
	private string nodeId = string.Empty;
	// 节点类型。
	private string nodeKind = string.Empty;

	/// <summary>
	/// 初始化按钮显示与拖拽载荷。
	/// </summary>
	public void Setup(string displayText, string inputNodeId, string inputNodeKind)
	{
		Text = displayText;
		TooltipText = inputNodeId;
		nodeId = inputNodeId;
		nodeKind = inputNodeKind;
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		Alignment = HorizontalAlignment.Left;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		Label dragPreview = new()
		{
			Text = Text,
			ThemeTypeVariation = "HeaderSmall"
		};
		SetDragPreview(dragPreview);

		Dictionary dragPayload = new()
		{
			["nodeId"] = nodeId,
			["nodeKind"] = nodeKind
		};
		return Variant.From(dragPayload);
	}
}
