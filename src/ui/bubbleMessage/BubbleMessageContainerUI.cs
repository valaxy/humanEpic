using Godot;

/// <summary>
/// 气泡消息容器。
/// 负责管理多个气泡消息的显示顺序和布局。
/// </summary>
[GlobalClass]
public partial class BubbleMessageContainerUI : CanvasLayer
{
	// 气泡消息场景资源。
	private PackedScene bubbleScene = GD.Load<PackedScene>("res://src/ui/bubbleMessage/bubble_message_ui.tscn");
	// 消息垂直列表容器。
	private VBoxContainer vBox = null!;

	public override void _Ready()
	{
		vBox = GetNode<VBoxContainer>("Control/VBoxContainer");
	}

	/// <summary>
	/// 添加并显示一条新的气泡消息。
	/// </summary>
	public void AddMessage(string message)
	{
		BubbleMessageUI bubble = bubbleScene.Instantiate<BubbleMessageUI>();
		vBox.AddChild(bubble);
		bubble.SetMessage($"{message} 事件触发");
		vBox.MoveChild(bubble, 0);
	}
}
