using Godot;
using System.Linq;

/// <summary>
/// 画布边视觉绘制器，封装边样式与绘制规则。
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
			.Where(edge => topologyCanvas.NodeLayout.ContainsKey(edge.FromNodeId) && topologyCanvas.NodeLayout.ContainsKey(edge.ToNodeId))
			.ToList()
			.ForEach(edge => drawEdge(canvas, topologyCanvas, edge));
	}

	// 绘制单条连线。
	private static void drawEdge(Node2D canvas, TopologyCanvas topologyCanvas, MetricEdge edge)
	{
		Vector2 fromPosition = topologyCanvas.NodeLayout[edge.FromNodeId];
		Vector2 toPosition = topologyCanvas.NodeLayout[edge.ToNodeId];
		Vector2 fromCenter = fromPosition + (topologyCanvas.NodeSize * edgeAnchorOffsetRatio);
		Vector2 toCenter = toPosition + (topologyCanvas.NodeSize * edgeAnchorOffsetRatio);
		Draw(canvas, fromCenter, toCenter);
	}
}
