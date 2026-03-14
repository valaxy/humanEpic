using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 整个游戏演化的复杂系统，负责管理多个 TopologyScope。
/// </summary>
public sealed class GameSystem
{
	/// <summary>
	/// 默认空系统。
	/// </summary>
	public static GameSystem Empty { get; } = new(Array.Empty<MetricNode>(), Array.Empty<MetricEdge>());

	// 当前作用域集合。
	private IReadOnlyList<Topology> scopes = Array.Empty<Topology>();
	// 当前完整指标节点集合。
	private IReadOnlyList<MetricNode> metrics = Array.Empty<MetricNode>();
	// 当前完整连线集合。
	private IReadOnlyList<MetricEdge> edges = Array.Empty<MetricEdge>();

	/// <summary>
	/// 当前作用域列表。
	/// </summary>
	public IReadOnlyList<Topology> Scopes => scopes;

	/// <summary>
	/// 当前完整指标节点集合。
	/// </summary>
	public IReadOnlyList<MetricNode> Metrics => metrics;

	/// <summary>
	/// 当前完整连线集合。
	/// </summary>
	public IReadOnlyList<MetricEdge> Edges => edges;

	/// <summary>
	/// 创建仅包含拓扑数据的系统对象。
	/// </summary>
	public GameSystem(IReadOnlyList<MetricNode> metrics, IReadOnlyList<MetricEdge> edges)
	{
		setScopes(metrics, edges);
	}

	/// <summary>
	/// 构建用于 UI 展示的作用域列表。
	/// </summary>
	public IReadOnlyList<Topology> BuildLayoutScopes()
	{
		return scopes.ToList();
	}

	/// <summary>
	/// 按类作用域筛选系统。
	/// </summary>
	public GameSystem FilterByOwnerType(string ownerTypeFullName)
	{
		Topology scope = scopes
			.Where(scopeItem => scopeItem.ScopeKey == ownerTypeFullName)
			.FirstOrDefault() ?? new Topology(ownerTypeFullName, getTypeShortName(ownerTypeFullName));
		return new GameSystem(scope.MetricNodes, scope.MetricEdges);
	}

	/// <summary>
	/// 收集系统中所有有效节点 ID。
	/// </summary>
	public IReadOnlyCollection<string> CollectMetricNodeIds()
	{
		return metrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);
	}

	/// <summary>
	/// 根据布局数据推导当前可激活节点集合。
	/// </summary>
	public HashSet<string> DeriveActiveNodeIds(IEnumerable<string> layoutNodeIds)
	{
		HashSet<string> validMetricNodeIds = metrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);
		HashSet<string> activeIds = layoutNodeIds
			.Where(validMetricNodeIds.Contains)
			.ToHashSet(StringComparer.Ordinal);

		return activeIds;
	}

	// 按当前指标节点和连线重建作用域集合。
	private void setScopes(IReadOnlyList<MetricNode> inputMetrics, IReadOnlyList<MetricEdge> inputEdges)
	{
		IReadOnlyList<MetricNode> allMetrics = inputMetrics.ToList();
		IReadOnlyList<MetricEdge> allEdges = inputEdges.ToList();
		metrics = allMetrics;
		edges = allEdges;
		scopes = allMetrics
			.Select(static metric => metric.OwnerTypeFullName)
			.Where(static ownerTypeName => string.IsNullOrWhiteSpace(ownerTypeName) == false)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static ownerTypeName => ownerTypeName, StringComparer.Ordinal)
			.Select(ownerTypeName => createScope(ownerTypeName!, allMetrics, allEdges))
			.ToList();
	}

	// 构建单个作用域并筛选其内部连线。
	private static Topology createScope(string ownerTypeFullName, IReadOnlyList<MetricNode> allMetrics, IReadOnlyList<MetricEdge> allEdges)
	{
		IReadOnlyList<MetricNode> scopedMetrics = allMetrics
			.Where(metric => metric.OwnerTypeFullName == ownerTypeFullName)
			.ToList();
		HashSet<string> scopedMetricNodeIds = scopedMetrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);
		IReadOnlyList<MetricEdge> scopedEdges = allEdges
			.Where(edge => scopedMetricNodeIds.Contains(edge.FromNodeId) && scopedMetricNodeIds.Contains(edge.ToNodeId))
			.ToList();
		return new Topology(ownerTypeFullName, getTypeShortName(ownerTypeFullName), scopedMetrics, scopedEdges);
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
