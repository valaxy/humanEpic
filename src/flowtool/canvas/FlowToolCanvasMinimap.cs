using Godot;
using System;

/// <summary>
/// 画布缩略图覆盖层，负责显示主摄像机视口框并处理导航点击。
/// </summary>
[GlobalClass]
public partial class FlowToolCanvasMinimap : Control
{
	// 边框颜色。
	private static readonly Color borderColor = new(0.35f, 0.45f, 0.56f, 0.95f);
	// 导航回调。
	private Action<Vector2> navigateRequested = static _ => { };

	/// <summary>
	/// 初始化默认交互属性。
	/// </summary>
	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
	}

	/// <summary>
	/// 配置缩略图与双摄像机上下文。
	/// </summary>
	public void Configure(
		SubViewport inputMainViewport,
		SubViewport inputMinimapViewport,
		Camera2D inputMainCamera,
		Camera2D inputMinimapCamera,
		Vector2 inputWorldSize,
		Action<Vector2> onNavigateRequested)
	{
		_ = inputMainViewport;
		_ = inputMinimapViewport;
		_ = inputMainCamera;
		_ = inputMinimapCamera;
		_ = inputWorldSize;
		navigateRequested = onNavigateRequested;
		QueueRedraw();
	}

	/// <summary>
	/// 触发缩略图覆盖层重绘。
	/// </summary>
	public void Refresh()
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, Size), borderColor, false, 1f);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton || mouseButton.ButtonIndex != MouseButton.Left || mouseButton.Pressed == false)
		{
			return;
		}

		if (new Rect2(Vector2.Zero, Size).HasPoint(mouseButton.Position) == false)
		{
			return;
		}

		navigateRequested(mouseButton.Position);
	}
}
