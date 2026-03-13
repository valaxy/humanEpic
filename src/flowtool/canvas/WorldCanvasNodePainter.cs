using Godot;
using System;

/// <summary>
/// 画布节点视觉绘制器，封装节点样式与文本绘制规则。
/// </summary>
public static class WorldCanvasNodePainter
{
	// 节点默认背景色。
	private static readonly Color defaultNodeBackgroundColor = new(0.12f, 0.16f, 0.2f);
	// 节点选中背景色。
	private static readonly Color selectedNodeBackgroundColor = new(0.16f, 0.24f, 0.3f);
	// 节点边框色。
	private static readonly Color nodeBorderColor = new(0.39f, 0.69f, 0.92f);
	// 标题文字颜色。
	private static readonly Color titleTextColor = new(0.95f, 0.97f, 1f);
	// 详情文字颜色。
	private static readonly Color detailTextColor = new(0.73f, 0.79f, 0.86f);

	/// <summary>
	/// 绘制单个节点。
	/// </summary>
	public static void Draw(Node2D canvas, MetricNode node, Vector2 position, Vector2 nodeSize, Font font, int fontSize, bool isSelected)
	{
		Color backgroundColor = isSelected ? selectedNodeBackgroundColor : defaultNodeBackgroundColor;
		Rect2 nodeRect = new(position, nodeSize);
		canvas.DrawRect(nodeRect, backgroundColor, true);
		canvas.DrawRect(nodeRect, nodeBorderColor, false, 2f);

		Vector2 titlePosition = position + new Vector2(12f, 28f);
		canvas.DrawString(font, titlePosition, node.DisplayName, HorizontalAlignment.Left, nodeSize.X - 24f, fontSize, titleTextColor);

		string detailText = node.CreateDetailText();
		Vector2 detailPosition = position + new Vector2(12f, 52f);
		canvas.DrawString(font, detailPosition, detailText, HorizontalAlignment.Left, nodeSize.X - 24f, Math.Max(fontSize - 2, 10), detailTextColor);
	}
}
