using Godot;
using System;

/// <summary>
/// 画布缩略图覆盖层，负责显示主摄像机视口框并处理导航点击。
/// </summary>
[Tool]
[GlobalClass]
public partial class FlowToolCanvasMinimap : Control
{
	// 边框颜色。
	private static readonly Color borderColor = new(0.35f, 0.45f, 0.56f, 0.95f);
	// 视口框颜色。
	private static readonly Color viewportRectColor = new(0.96f, 0.91f, 0.56f, 0.95f);

	// 主视口。
	private SubViewport mainViewport = null!;
	// 缩略图视口。
	private SubViewport minimapViewport = null!;
	// 主摄像机。
	private Camera2D mainCamera = null!;
	// 缩略图摄像机。
	private Camera2D minimapCamera = null!;
	// 画布世界尺寸。
	private Vector2 worldSize = new(1f, 1f);
	// 导航请求回调。
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
		mainViewport = inputMainViewport;
		minimapViewport = inputMinimapViewport;
		mainCamera = inputMainCamera;
		minimapCamera = inputMinimapCamera;
		worldSize = new Vector2(Mathf.Max(inputWorldSize.X, 1f), Mathf.Max(inputWorldSize.Y, 1f));
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
		if (isConfigured() == false)
		{
			return;
		}

		DrawRect(new Rect2(Vector2.Zero, Size), borderColor, false, 1f);
		drawMainViewportRect();
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (isConfigured() == false)
		{
			return;
		}

		if (@event is not InputEventMouseButton mouseButton || mouseButton.ButtonIndex != MouseButton.Left || mouseButton.Pressed == false)
		{
			return;
		}

		if (new Rect2(Vector2.Zero, Size).HasPoint(mouseButton.Position) == false)
		{
			return;
		}

		Vector2 worldTarget = minimapScreenToWorld(mouseButton.Position);
		navigateRequested(worldTarget);
	}

	// 判断当前是否已完成依赖注入。
	private bool isConfigured()
	{
		return mainViewport != null && minimapViewport != null && mainCamera != null && minimapCamera != null;
	}

	// 绘制主摄像机视口在缩略图中的覆盖框。
	private void drawMainViewportRect()
	{
		Vector2 mainViewportSize = new(
			Mathf.Max(mainViewport.Size.X, 1),
			Mathf.Max(mainViewport.Size.Y, 1));
		Vector2 mainHalfSize = new(
			mainViewportSize.X * 0.5f * mainCamera.Zoom.X,
			mainViewportSize.Y * 0.5f * mainCamera.Zoom.Y);
		Vector2 mainTopLeftWorld = mainCamera.Position - mainHalfSize;
		Vector2 mainBottomRightWorld = mainCamera.Position + mainHalfSize;

		Vector2 minimapTopLeftScreen = worldToMinimapScreen(mainTopLeftWorld);
		Vector2 minimapBottomRightScreen = worldToMinimapScreen(mainBottomRightWorld);
		Rect2 viewportRect = new(minimapTopLeftScreen, minimapBottomRightScreen - minimapTopLeftScreen);
		DrawRect(viewportRect, viewportRectColor, false, 2f);
	}

	// 将世界坐标映射到缩略图屏幕坐标。
	private Vector2 worldToMinimapScreen(Vector2 worldPosition)
	{
		Vector2 minimapViewportSize = new(
			Mathf.Max(minimapViewport.Size.X, 1),
			Mathf.Max(minimapViewport.Size.Y, 1));
		Vector2 minimapHalfSize = new(
			minimapViewportSize.X * 0.5f * minimapCamera.Zoom.X,
			minimapViewportSize.Y * 0.5f * minimapCamera.Zoom.Y);
		Vector2 minimapTopLeftWorld = minimapCamera.Position - minimapHalfSize;
		Vector2 local = new(
			(worldPosition.X - minimapTopLeftWorld.X) / Mathf.Max(minimapCamera.Zoom.X, 0.0001f),
			(worldPosition.Y - minimapTopLeftWorld.Y) / Mathf.Max(minimapCamera.Zoom.Y, 0.0001f));
		return new Vector2(
			Mathf.Clamp(local.X, 0f, Size.X),
			Mathf.Clamp(local.Y, 0f, Size.Y));
	}

	// 将缩略图屏幕坐标映射回世界坐标。
	private Vector2 minimapScreenToWorld(Vector2 minimapScreenPosition)
	{
		Vector2 minimapViewportSize = new(
			Mathf.Max(minimapViewport.Size.X, 1),
			Mathf.Max(minimapViewport.Size.Y, 1));
		Vector2 minimapHalfSize = new(
			minimapViewportSize.X * 0.5f * minimapCamera.Zoom.X,
			minimapViewportSize.Y * 0.5f * minimapCamera.Zoom.Y);
		Vector2 minimapTopLeftWorld = minimapCamera.Position - minimapHalfSize;
		Vector2 world = new(
			minimapTopLeftWorld.X + minimapScreenPosition.X * minimapCamera.Zoom.X,
			minimapTopLeftWorld.Y + minimapScreenPosition.Y * minimapCamera.Zoom.Y);

		return new Vector2(
			Mathf.Clamp(world.X, 0f, worldSize.X),
			Mathf.Clamp(world.Y, 0f, worldSize.Y));
	}
}
