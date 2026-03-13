using Godot;
using Godot.Collections;

/// <summary>
/// 右侧未分配池中的可拖拽节点按钮。
/// </summary>
public partial class UnassignedItem : Button
{
	// 节点 ID。
	private string nodeId = string.Empty;

	/// <summary>
	/// 初始化按钮显示与拖拽载荷。
	/// </summary>
	public void Setup(string displayText, string inputNodeId)
	{
		Text = displayText;
		TooltipText = inputNodeId;
		nodeId = inputNodeId;
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		Alignment = HorizontalAlignment.Left;
	}

	// 拖拽到canvas里，由canvas接管并处理
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
			["displayText"] = Text
		};
		return Variant.From(dragPayload);
	}
}
