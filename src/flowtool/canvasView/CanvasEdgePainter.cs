using Godot;

/// <summary>
/// 画布边视觉绘制器，封装边样式与绘制规则。
/// </summary>
public static class CanvasEdgePainter
{
	// 默认边颜色。
	private static readonly Color defaultEdgeColor = new(0.46f, 0.74f, 0.95f);
	// 默认边宽。
	private const float defaultEdgeWidth = 2f;

	/// <summary>
	/// 绘制单条边。
	/// </summary>
	public static void Draw(Node2D canvas, Vector2 fromCenter, Vector2 toCenter)
	{
		canvas.DrawLine(fromCenter, toCenter, defaultEdgeColor, defaultEdgeWidth, true);
	}
}
