using Godot;

/// <summary>
/// 气泡消息容器演示入口。
/// 通过定时器持续模拟消息弹出。
/// </summary>
[GlobalClass]
public partial class BubbleMessageDemo : CanvasLayer
{
	// 气泡消息容器节点。
	private BubbleMessageContainerUI bubbleContainer = null!;
	// 演示消息定时器。
	private Timer messageTimer = null!;
	// 当前消息序号。
	private int messageIndex = 1;

	public override void _Ready()
	{
		bubbleContainer = GetNode<BubbleMessageContainerUI>("BubbleMessageContainerUI");
		messageTimer = GetNode<Timer>("MessageTimer");
		messageTimer.Timeout += onMessageTimerTimeout;
		onMessageTimerTimeout();
	}

	// 定时生成一条新消息。
	private void onMessageTimerTimeout()
	{
		bubbleContainer.AddMessage($"第{messageIndex}条");
		messageIndex += 1;
	}
}
