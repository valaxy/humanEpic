using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 画布缩略图组件，负责展示节点全局分布并提供快速定位能力。
/// </summary>
[Tool]
[GlobalClass]
public partial class FlowToolCanvasMinimap : Control
{
	// 缩略图内边距。
	private const float innerPadding = 8f;
	// 节点示意最小尺寸。
	private const float minDotSize = 4f;
	// 背景颜色。
	private static readonly Color backgroundColor = new(0.05f, 0.07f, 0.1f, 0.85f);
	// 边框颜色。
	private static readonly Color borderColor = new(0.35f, 0.45f, 0.56f, 1f);
	// 节点颜色。
	private static readonly Color nodeColor = new(0.44f, 0.74f, 0.95f, 0.95f);
	// 视口框颜色。
	private static readonly Color viewportRectColor = new(0.96f, 0.91f, 0.56f, 0.95f);

	// 当前节点布局快照。
	private IReadOnlyDictionary<string, Vector2> layoutByNodeId = new Dictionary<string, Vector2>(StringComparer.Ordinal);
	// 当前世界边界。
	private Rect2 worldBounds = new(Vector2.Zero, new Vector2(1f, 1f));
	// 当前视口尺寸。
	private Vector2 viewportSize = new(1f, 1f);
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
	/// 绑定缩略图导航回调。
	/// </summary>
	public void SetNavigateRequested(Action<Vector2> onNavigateRequested)
	{
		navigateRequested = onNavigateRequested;
	}

	/// <summary>
	/// 刷新缩略图展示数据。
	/// </summary>
	public void UpdateSnapshot(IReadOnlyDictionary<string, Vector2> nodeLayout, Vector2 inputViewportSize, Vector2 inputWorldSize)
	{
		layoutByNodeId = nodeLayout
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		viewportSize = new Vector2(Mathf.Max(inputViewportSize.X, 1f), Mathf.Max(inputViewportSize.Y, 1f));
		worldBounds = new Rect2(
			Vector2.Zero,
			new Vector2(
				Mathf.Max(inputWorldSize.X, viewportSize.X),
				Mathf.Max(inputWorldSize.Y, viewportSize.Y)));
		QueueRedraw();
	}

	public override void _Draw()
	{
		Rect2 minimapRect = new(Vector2.Zero, Size);
		DrawRect(minimapRect, backgroundColor, true);
		DrawRect(minimapRect, borderColor, false, 1f);

		Rect2 contentRect = createContentRect();
		drawNodeDots(contentRect);
		drawViewportRect(contentRect);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton || mouseButton.ButtonIndex != MouseButton.Left || mouseButton.Pressed == false)
		{
			return;
		}

		Rect2 contentRect = createContentRect();
		if (contentRect.HasPoint(mouseButton.Position) == false)
		{
			return;
		}

		Vector2 normalized = new(
			(mouseButton.Position.X - contentRect.Position.X) / Mathf.Max(contentRect.Size.X, 1f),
			(mouseButton.Position.Y - contentRect.Position.Y) / Mathf.Max(contentRect.Size.Y, 1f));
		Vector2 worldTarget = new(
			worldBounds.Position.X + normalized.X * worldBounds.Size.X,
			worldBounds.Position.Y + normalized.Y * worldBounds.Size.Y);
		navigateRequested(worldTarget);
	}

	// 创建缩略图内容区域。
	private Rect2 createContentRect()
	{
		float width = Mathf.Max(Size.X - innerPadding * 2f, 1f);
		float height = Mathf.Max(Size.Y - innerPadding * 2f, 1f);
		return new Rect2(new Vector2(innerPadding, innerPadding), new Vector2(width, height));
	}

	// 绘制所有节点示意点。
	private void drawNodeDots(Rect2 contentRect)
	{
		layoutByNodeId
			.Values
			.ToList()
			.ForEach(position =>
			{
				Vector2 ratio = toContentRatio(position);
				Vector2 dotPos = new(
					contentRect.Position.X + ratio.X * contentRect.Size.X,
					contentRect.Position.Y + ratio.Y * contentRect.Size.Y);
				Rect2 dotRect = new(dotPos - new Vector2(minDotSize * 0.5f, minDotSize * 0.5f), new Vector2(minDotSize, minDotSize));
				DrawRect(dotRect, nodeColor, true);
			});
	}

	// 绘制当前视口边界示意。
	private void drawViewportRect(Rect2 contentRect)
	{
		Vector2 viewportRatio = new(
			Mathf.Clamp(viewportSize.X / Mathf.Max(worldBounds.Size.X, 1f), 0.05f, 1f),
			Mathf.Clamp(viewportSize.Y / Mathf.Max(worldBounds.Size.Y, 1f), 0.05f, 1f));
		Rect2 viewportRect = new(
			contentRect.Position,
			new Vector2(contentRect.Size.X * viewportRatio.X, contentRect.Size.Y * viewportRatio.Y));
		DrawRect(viewportRect, viewportRectColor, false, 2f);
	}

	// 将世界坐标映射到缩略图归一化坐标。
	private Vector2 toContentRatio(Vector2 worldPosition)
	{
		float normalizedX = (worldPosition.X - worldBounds.Position.X) / Mathf.Max(worldBounds.Size.X, 1f);
		float normalizedY = (worldPosition.Y - worldBounds.Position.Y) / Mathf.Max(worldBounds.Size.Y, 1f);
		return new Vector2(
			Mathf.Clamp(normalizedX, 0f, 1f),
			Mathf.Clamp(normalizedY, 0f, 1f));
	}

}
