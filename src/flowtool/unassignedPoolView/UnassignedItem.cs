using Godot;
using Godot.Collections;

/// <summary>
/// 未分配池中的可拖拽节点按钮。
/// </summary>
public partial class UnassignedItem : Button
{
	// 拖拽载荷中的节点 ID 键。
	private static readonly StringName dragNodeIdKey = "nodeId";
	// 拖拽载荷中的显示名键。
	private static readonly StringName dragDisplayTextKey = "displayText";

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
			[dragNodeIdKey] = nodeId,
			[dragDisplayTextKey] = Text
		};
		return dragPayload;
	}
}
