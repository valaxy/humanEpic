using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 整个游戏演化的复杂系统，负责管理多个 TopologyScope。
/// </summary>
public sealed class GameSystem
{
	/// <summary>
	/// 全量作用域键。
	/// </summary>
	public const string AllTopologyScopeKey = "all";

	/// <summary>
	/// 默认空系统。
	/// </summary>
	public static GameSystem Empty { get; } = new(Array.Empty<MetricNode>(), Array.Empty<MetricEdge>());

	// 全量拓扑。
	private readonly Topology allTopology;
	// 作用域拓扑集合。
	private readonly IReadOnlyList<Topology> scopedTopologies;
	// 按 scopeKey 索引的拓扑映射。
	private readonly IReadOnlyDictionary<string, Topology> scopedTopologyByScopeKey;

	/// <summary>
	/// 当前作用域列表。
	/// </summary>
	public IReadOnlyList<Topology> Scopes => scopedTopologies;

	/// <summary>
	/// 当前完整指标节点集合。
	/// </summary>
	public IReadOnlyList<MetricNode> Metrics => allTopology.MetricNodes;

	/// <summary>
	/// 当前完整连线集合。
	/// </summary>
	public IReadOnlyList<MetricEdge> Edges => allTopology.MetricEdges;

	/// <summary>
	/// 创建仅包含拓扑数据的系统对象。
	/// </summary>
	public GameSystem(IReadOnlyList<MetricNode> metrics, IReadOnlyList<MetricEdge> edges)
	{
		IReadOnlyList<MetricNode> allMetrics = metrics.ToList();
		IReadOnlyList<MetricEdge> allEdges = edges.ToList();
		allTopology = new Topology(AllTopologyScopeKey, "全部", allMetrics, allEdges);

		scopedTopologies = allMetrics
			.Select(static metric => metric.OwnerTypeFullName)
			.Where(static ownerTypeName => string.IsNullOrWhiteSpace(ownerTypeName) == false)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static ownerTypeName => ownerTypeName, StringComparer.Ordinal)
			.Select(ownerTypeName => createScopeTopology(ownerTypeName!, allMetrics, allEdges))
			.ToList();

		scopedTopologyByScopeKey = scopedTopologies
			.ToDictionary(static scope => scope.ScopeKey, static scope => scope, StringComparer.Ordinal);
	}

	/// <summary>
	/// 构建用于 UI 展示的作用域列表。
	/// </summary>
	public IReadOnlyList<Topology> BuildLayoutScopes()
	{
		return new[] { allTopology }
			.Concat(scopedTopologies)
			.ToList();
	}

	/// <summary>
	/// 按类作用域筛选系统。
	/// </summary>
	public Topology GetTopology(string scopeKey)
	{
		if (scopeKey == AllTopologyScopeKey)
		{
			return allTopology;
		}

		return scopedTopologyByScopeKey.TryGetValue(scopeKey, out Topology? topology)
			? topology
			: new Topology(scopeKey, getTypeShortName(scopeKey));
	}

	/// <summary>
	/// 收集系统中所有有效节点 ID。
	/// </summary>
	public IReadOnlyCollection<string> CollectMetricNodeIds()
	{
		return allTopology.CollectMetricNodeIds();
	}

	/// <summary>
	/// 根据布局数据推导当前可激活节点集合。
	/// </summary>
	public HashSet<string> DeriveActiveNodeIds(IEnumerable<string> layoutNodeIds, Topology topology)
	{
		HashSet<string> validMetricNodeIds = topology
			.CollectMetricNodeIds()
			.ToHashSet(StringComparer.Ordinal);
		HashSet<string> activeIds = layoutNodeIds
			.Where(validMetricNodeIds.Contains)
			.ToHashSet(StringComparer.Ordinal);

		return activeIds;
	}

	// 构建单个作用域并筛选其内部连线。
	private static Topology createScopeTopology(string ownerTypeFullName, IReadOnlyList<MetricNode> allMetrics, IReadOnlyList<MetricEdge> allEdges)
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
