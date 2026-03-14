using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 整个游戏演化的复杂系统，负责管理多个 MetricScope。
/// </summary>
public sealed class GameSystem
{
	// 作用域集合。
	private readonly Dictionary<string, MetricScope> scopes;

	/// <summary>
	/// 当前作用域列表。
	/// </summary>
	public IDictionary<string, MetricScope> Scopes => scopes;


	/// <summary>
	/// 创建仅包含拓扑数据的系统对象。
	/// </summary>
	public GameSystem(IEnumerable<MetricScope> scopes)
	{
		this.scopes = scopes.ToDictionary(scope => scope.Name, StringComparer.Ordinal);
	}


	// /// <summary>
	// /// 根据布局数据推导当前可激活节点集合。
	// /// </summary>
	// public HashSet<string> DeriveActiveNodeIds(IEnumerable<string> layoutNodeIds, Topology topology)
	// {
	// 	HashSet<string> validMetricNodeIds = topology
	// 		.CollectMetricNodeIds()
	// 		.ToHashSet(StringComparer.Ordinal);
	// 	HashSet<string> activeIds = layoutNodeIds
	// 		.Where(validMetricNodeIds.Contains)
	// 		.ToHashSet(StringComparer.Ordinal);

	// 	return activeIds;
	// }

	// // 构建单个作用域并筛选其内部连线。
	// private Topology createScopeTopology(string ownerTypeFullName, IReadOnlyList<MetricNode> allMetrics, IReadOnlyList<MetricEdge> allEdges)
	// {
	// 	IReadOnlyList<MetricNode> scopedMetrics = allMetrics
	// 		.Where(metric => metric.OwnerTypeFullName == ownerTypeFullName)
	// 		.ToList();
	// 	HashSet<string> scopedMetricNodeIds = scopedMetrics
	// 		.Select(static metric => metric.NodeId)
	// 		.ToHashSet(StringComparer.Ordinal);
	// 	IReadOnlyList<MetricEdge> scopedEdges = allEdges
	// 		.Where(edge => scopedMetricNodeIds.Contains(edge.FromNodeId) && scopedMetricNodeIds.Contains(edge.ToNodeId))
	// 		.ToList();
	// 	return new Topology(ownerTypeFullName, getTypeShortName(ownerTypeFullName), scopedMetrics, scopedEdges);
	// }
}
