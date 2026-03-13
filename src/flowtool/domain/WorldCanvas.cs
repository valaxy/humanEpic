using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 画布领域实体，负责管理画布尺寸、节点尺寸、节点布局与连线数据。
/// </summary>
public sealed class WorldCanvas
{
	/// <summary>
	/// 默认空画布。
	/// </summary>
	public static WorldCanvas Empty { get; } = new(Array.Empty<MetricNode>(), Array.Empty<MetricEdge>());

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
	/// 指标节点集合。
	/// </summary>
	public IReadOnlyList<MetricNode> Metrics { get; private set; } = Array.Empty<MetricNode>();

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
	/// 创建画布领域对象。
	/// </summary>
	public WorldCanvas(float width, float height, float inputNodeWidth, float inputNodeHeight)
	{
		Width = Mathf.Max(width, 1f);
		Height = Mathf.Max(height, 1f);
		NodeWidth = Mathf.Max(inputNodeWidth, 1f);
		NodeHeight = Mathf.Max(inputNodeHeight, 1f);
	}

	/// <summary>
	/// 创建仅包含拓扑数据的画布领域对象。
	/// </summary>
	public WorldCanvas(IReadOnlyList<MetricNode> metrics, IReadOnlyList<MetricEdge> edges)
		: this(defaultWidth, defaultHeight, defaultNodeWidth, defaultNodeHeight)
	{
		Metrics = metrics.ToList();
		Nodes = Metrics.ToDictionary(metric => metric.NodeId, metric => metric, StringComparer.Ordinal);
		Edges = edges.ToList();
	}

	/// <summary>
	/// 刷新当前节点、连线与布局快照。
	/// </summary>
	public void UpdateGraph(
		IReadOnlyDictionary<string, MetricNode> nodesByNodeId,
		IReadOnlyDictionary<string, Vector2> layoutByNodeId,
		IReadOnlyList<MetricEdge> activeEdges)
	{
		Metrics = nodesByNodeId
			.Select(static pair => pair.Value)
			.ToList();
		Nodes = nodesByNodeId
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		NodeLayout = layoutByNodeId
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		Edges = activeEdges.ToList();
	}

	/// <summary>
	/// 构建当前拓扑的布局作用域列表。
	/// </summary>
	public IReadOnlyList<FlowToolLayoutScopeItem> BuildLayoutScopes()
	{
		return Metrics
			.Select(static metric => metric.OwnerTypeFullName)
			.Where(static ownerTypeName => string.IsNullOrWhiteSpace(ownerTypeName) == false)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static ownerTypeName => ownerTypeName, StringComparer.Ordinal)
			.Select(static ownerTypeName => new FlowToolLayoutScopeItem(ownerTypeName!, getTypeShortName(ownerTypeName!)))
			.ToList();
	}

	/// <summary>
	/// 按类作用域筛选拓扑。
	/// </summary>
	public WorldCanvas FilterByOwnerType(string ownerTypeFullName)
	{
		IReadOnlyList<MetricNode> scopedMetrics = Metrics
			.Where(metric => metric.OwnerTypeFullName == ownerTypeFullName)
			.ToList();
		HashSet<string> scopedMetricNodeIds = scopedMetrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);

		IReadOnlyList<MetricEdge> scopedEdges = Edges
			.Where(edge => scopedMetricNodeIds.Contains(edge.FromNodeId) && scopedMetricNodeIds.Contains(edge.ToNodeId))
			.ToList();

		return new WorldCanvas(scopedMetrics, scopedEdges);
	}

	/// <summary>
	/// 收集当前拓扑中所有有效节点 ID。
	/// </summary>
	public IReadOnlyCollection<string> CollectMetricNodeIds()
	{
		return Metrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);
	}

	/// <summary>
	/// 根据布局数据推导当前可激活节点集合。
	/// </summary>
	public HashSet<string> DeriveActiveNodeIds(IEnumerable<string> layoutNodeIds)
	{
		HashSet<string> validMetricNodeIds = Metrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);
		HashSet<string> activeIds = layoutNodeIds
			.Where(validMetricNodeIds.Contains)
			.ToHashSet(StringComparer.Ordinal);

		return activeIds;
	}

	// 获取类型短名。
	private static string getTypeShortName(string fullTypeName)
	{
		if (string.IsNullOrWhiteSpace(fullTypeName))
		{
			return string.Empty;
		}

		string[] segments = fullTypeName.Split('.', StringSplitOptions.RemoveEmptyEntries);
		return segments.LastOrDefault() ?? fullTypeName;
	}
}
