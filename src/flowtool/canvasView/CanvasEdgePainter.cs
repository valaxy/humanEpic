using Godot;
using System.Linq;

/// <summary>
/// 画布边视觉绘制器。
/// </summary>
public static class CanvasEdgePainter
{
	// 默认边颜色。
	private static readonly Color defaultEdgeColor = new(0.46f, 0.74f, 0.95f);
	// 默认边宽。
	private const float defaultEdgeWidth = 2f;
	// 连线起止点偏移比例。
	private const float edgeAnchorOffsetRatio = 0.5f;

	/// <summary>
	/// 绘制单条边。
	/// </summary>
	public static void Draw(Node2D canvas, Vector2 fromCenter, Vector2 toCenter)
	{
		canvas.DrawLine(fromCenter, toCenter, defaultEdgeColor, defaultEdgeWidth, true);
	}

	/// <summary>
	/// 绘制画布中的所有连线。
	/// </summary>
	public static void DrawEdges(Node2D canvas, TopologyCanvas topologyCanvas)
	{
		topologyCanvas.Edges
			.Where(edge => topologyCanvas.Nodes.ContainsKey(edge.FromNode.Id) && topologyCanvas.Nodes.ContainsKey(edge.ToNode.Id))
			.Where(edge => edge.FromNode.IsActive && edge.ToNode.IsActive)
			.ToList()
			.ForEach(edge => drawEdge(canvas, topologyCanvas, edge));
	}

	// 绘制单条连线。
	private static void drawEdge(Node2D canvas, TopologyCanvas topologyCanvas, TopologyEdge edge)
	{
		Vector2 fromPosition = edge.FromNode.Position;
		Vector2 toPosition = edge.ToNode.Position;
		Vector2 fromCenter = fromPosition + (topologyCanvas.NodeSize * edgeAnchorOffsetRatio);
		Vector2 toCenter = toPosition + (topologyCanvas.NodeSize * edgeAnchorOffsetRatio);
		Draw(canvas, fromCenter, toCenter);
	}
}
