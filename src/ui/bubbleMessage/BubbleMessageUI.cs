using Godot;

/// <summary>
/// 单个气泡消息组件。
/// 用于显示简短通知消息，支持自动消失和透明度渐变。
/// </summary>
[GlobalClass]
public partial class BubbleMessageUI : PanelContainer
{
	/// <summary>
	/// 消息关闭时触发。
	/// </summary>
	[Signal]
	public delegate void ClosedEventHandler();

	// 消息文本标签。
	private Label label = null!;
	// 自动消失计时器。
	private Timer timer = null!;
	// 是否正在执行关闭动画。
	private bool isClosing;

	public override void _Ready()
	{
		label = GetNode<Label>("MarginContainer/Label");
		timer = GetNode<Timer>("Timer");
		timer.Timeout += onTimerTimeout;
		timer.Start(3.0);
	}

	/// <summary>
	/// 设置并展示消息内容。
	/// </summary>
	public void SetMessage(string text)
	{
		label.Text = text;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			closeMessage();
		}
	}

	// 计时器结束时关闭消息。
	private void onTimerTimeout()
	{
		closeMessage();
	}

	// 执行关闭过程，触发淡出动画后清理自身。
	private void closeMessage()
	{
		if (isClosing)
		{
			return;
		}

		isClosing = true;
		Tween tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
		tween.Finished += onTweenFinished;
	}

	// 淡出完成后的收尾逻辑。
	private void onTweenFinished()
	{
		EmitSignal(SignalName.Closed);
		QueueFree();
	}
}
