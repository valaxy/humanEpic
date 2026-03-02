using Godot;

/// <summary>
/// 游戏时间显示组件，实时展示天数与小时。
/// </summary>
[GlobalClass]
public partial class TimeDisplayUI : PanelContainer
{
	// 时间文本标签。
	private Label label = null!;
	// 绑定的时间系统。
	private TimeSystem timeSystem = null!;

	public override void _Ready()
	{
		label = GetNode<Label>("%TimeLabel");
	}

	/// <summary>
	/// 绑定时间系统。
	/// </summary>
	public void Setup(TimeSystem systemData)
	{
		timeSystem = systemData;
		refreshLabel();
	}

	public override void _Process(double delta)
	{
		if (timeSystem == null)
		{
			return;
		}

		refreshLabel();
	}

	// 刷新显示文本。
	private void refreshLabel()
	{
		int day = timeSystem.GetDay();
		int hour = timeSystem.GetHour();
		label.Text = $"第 {day} 天 {hour:00} 时";
	}
}
