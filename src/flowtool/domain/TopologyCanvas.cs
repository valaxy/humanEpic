using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 拓扑画布模型，负责管理画布尺寸、节点尺寸与布局数据。
/// </summary>
public sealed class TopologyCanvas
{
	// 默认画布宽度。
	private const float defaultWidth = 5000f;
	// 默认画布高度。
	private const float defaultHeight = 3200f;
	// 默认节点宽度。
	private const float defaultNodeWidth = 280f;
	// 默认节点高度。
	private const float defaultNodeHeight = 96f;

	/// <summary>
	/// 虚拟画布宽度。
	/// </summary>
	public float Width { get; }

	/// <summary>
	/// 虚拟画布高度。
	/// </summary>
	public float Height { get; }

	/// <summary>
	/// 节点宽度。
	/// </summary>
	public float NodeWidth { get; }

	/// <summary>
	/// 节点高度。
	/// </summary>
	public float NodeHeight { get; }

	/// <summary>
	/// 画布尺寸。
	/// </summary>
	public Vector2 CanvasSize => new(Width, Height);

	/// <summary>
	/// 节点尺寸。
	/// </summary>
	public Vector2 NodeSize => new(NodeWidth, NodeHeight);

	/// <summary>
	/// 当前节点定义映射。
	/// </summary>
	public IReadOnlyDictionary<string, MetricNode> Nodes { get; private set; } =
		new Dictionary<string, MetricNode>(StringComparer.Ordinal);

	/// <summary>
	/// 当前节点布局映射。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> NodeLayout { get; private set; } =
		new Dictionary<string, Vector2>(StringComparer.Ordinal);

	/// <summary>
	/// 当前连线。
	/// </summary>
	public IReadOnlyList<MetricEdge> Edges { get; private set; } = Array.Empty<MetricEdge>();

	/// <summary>
	/// 创建拓扑画布。
	/// </summary>
	public TopologyCanvas(float width = defaultWidth, float height = defaultHeight, float inputNodeWidth = defaultNodeWidth, float inputNodeHeight = defaultNodeHeight)
	{
		Width = Mathf.Max(width, 1f);
		Height = Mathf.Max(height, 1f);
		NodeWidth = Mathf.Max(inputNodeWidth, 1f);
		NodeHeight = Mathf.Max(inputNodeHeight, 1f);
	}

	/// <summary>
	/// 刷新当前节点、连线与布局快照。
	/// </summary>
	public void UpdateGraph(
		IReadOnlyDictionary<string, MetricNode> nodesByNodeId,
		IReadOnlyDictionary<string, Vector2> layoutByNodeId,
		IReadOnlyList<MetricEdge> activeEdges)
	{
		Nodes = nodesByNodeId
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		NodeLayout = layoutByNodeId
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		Edges = activeEdges.ToList();
	}

	/// <summary>
	/// 基于作用域拓扑与激活节点重建渲染快照。
	/// </summary>
	public (IReadOnlyDictionary<string, MetricNode> NodesByNodeId, IReadOnlyDictionary<string, Vector2> LayoutByNodeId, IReadOnlyList<MetricEdge> ActiveEdges) BuildScopeSnapshot(
		GameSystem scopedTopology,
		IReadOnlyCollection<string> activeNodeIds,
		IReadOnlyDictionary<string, Vector2> currentLayoutPositions,
		Vector2 fallbackPosition)
	{
		Dictionary<string, MetricNode> nodesByNodeId = scopedTopology.Metrics
			.Where(metric => activeNodeIds.Contains(metric.NodeId))
			.ToDictionary(metric => metric.NodeId, metric => metric, StringComparer.Ordinal);

		Dictionary<string, Vector2> layoutByNodeId = activeNodeIds
			.ToDictionary(
				nodeId => nodeId,
				nodeId => currentLayoutPositions.TryGetValue(nodeId, out Vector2 savedPosition) ? savedPosition : fallbackPosition,
				StringComparer.Ordinal);

		IReadOnlyList<MetricEdge> activeEdges = scopedTopology.Edges
			.Where(edge => activeNodeIds.Contains(edge.FromNodeId) && activeNodeIds.Contains(edge.ToNodeId))
			.ToList();

		return (nodesByNodeId, layoutByNodeId, activeEdges);
	}

	/// <summary>
	/// 尝试根据画布坐标拾取节点。
	/// </summary>
	public bool TryPickNodeIdAt(Vector2 canvasPosition, out string nodeId)
	{
		nodeId = NodeLayout
			.Where(pair => new Rect2(pair.Value, NodeSize).HasPoint(canvasPosition))
			.Select(static pair => pair.Key)
			.FirstOrDefault() ?? string.Empty;
		return string.IsNullOrWhiteSpace(nodeId) == false;
	}
}
