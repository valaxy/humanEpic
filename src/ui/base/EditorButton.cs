using Godot;

/// <summary>
/// 通用编辑器功能按钮。
/// 支持选中（开启）和未选中（关闭）两种状态样式切换。
/// </summary>
[GlobalClass]
public partial class EditorButton : Button
{
	/// <summary>
	/// 激活状态下的颜色。
	/// </summary>
	[Export]
	public Color ActiveColor { get; set; } = new(1.0f, 0.8f, 0.2f, 1.0f);

	/// <summary>
	/// 非激活状态下的颜色。
	/// </summary>
	[Export]
	public Color InactiveColor { get; set; } = new(1.0f, 1.0f, 1.0f, 1.0f);

	// 当前激活状态。
	private bool isActive;

	/// <summary>
	/// 当前是否处于激活状态。
	/// </summary>
	[Export]
	public bool IsActive
	{
		get => isActive;
		set
		{
			isActive = value;
			updateStyle();
		}
	}

	public override void _Ready()
	{
		updateStyle();
	}

	/// <summary>
	/// 设置按钮文本。
	/// </summary>
	public void SetLabel(string textValue)
	{
		Text = textValue;
	}

	// 根据活动状态更新按钮样式。
	private void updateStyle()
	{
		if (isActive)
		{
			AddThemeColorOverride("font_color", Colors.Black);
			AddThemeColorOverride("font_hover_color", Colors.Black);
			AddThemeColorOverride("font_pressed_color", Colors.Black);
			AddThemeColorOverride("font_focus_color", Colors.Black);

			StyleBoxFlat style = new();
			style.BgColor = ActiveColor;
			style.CornerRadiusTopLeft = 4;
			style.CornerRadiusTopRight = 4;
			style.CornerRadiusBottomLeft = 4;
			style.CornerRadiusBottomRight = 4;
			AddThemeStyleboxOverride("normal", style);
			AddThemeStyleboxOverride("hover", style);
			AddThemeStyleboxOverride("pressed", style);
			return;
		}

		RemoveThemeColorOverride("font_color");
		RemoveThemeColorOverride("font_hover_color");
		RemoveThemeColorOverride("font_pressed_color");
		RemoveThemeColorOverride("font_focus_color");

		RemoveThemeStyleboxOverride("normal");
		RemoveThemeStyleboxOverride("hover");
		RemoveThemeStyleboxOverride("pressed");
	}
}
