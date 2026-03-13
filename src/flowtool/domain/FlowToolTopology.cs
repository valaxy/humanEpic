using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// flowtool 拓扑快照。
/// </summary>
public sealed class FlowToolTopology
{
	/// <summary>
	/// 空拓扑实例。
	/// </summary>
	public static FlowToolTopology Empty { get; } = new(Array.Empty<FlowToolMetricNode>(), Array.Empty<FlowToolEdge>());

	/// <summary>
	/// 指标节点集合。
	/// </summary>
	public IReadOnlyList<FlowToolMetricNode> Metrics { get; }

	/// <summary>
	/// 关系边集合。
	/// </summary>
	public IReadOnlyList<FlowToolEdge> Edges { get; }

	/// <summary>
	/// 创建拓扑实例。
	/// </summary>
	public FlowToolTopology(IReadOnlyList<FlowToolMetricNode> metrics, IReadOnlyList<FlowToolEdge> edges)
	{
		Metrics = metrics;
		Edges = edges;
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
	public FlowToolTopology FilterByOwnerType(string ownerTypeFullName)
	{
		IReadOnlyList<FlowToolMetricNode> scopedMetrics = Metrics
			.Where(metric => metric.OwnerTypeFullName == ownerTypeFullName)
			.ToList();
		HashSet<string> scopedMetricNodeIds = scopedMetrics
			.Select(static metric => metric.NodeId)
			.ToHashSet(StringComparer.Ordinal);

		IReadOnlyList<FlowToolEdge> scopedEdges = Edges
			.Where(edge => scopedMetricNodeIds.Contains(edge.FromNodeId) && scopedMetricNodeIds.Contains(edge.ToNodeId))
			.ToList();

		return new FlowToolTopology(scopedMetrics, scopedEdges);
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

/// <summary>
/// flowtool 指标节点定义。
/// </summary>
public sealed class FlowToolMetricNode
{
	/// <summary>
	/// 节点唯一 ID。
	/// </summary>
	public string NodeId { get; }

	/// <summary>
	/// 指标英文名（方法名）。
	/// </summary>
	public string MetricName { get; }

	/// <summary>
	/// 指标显示名（通常来自 XML 注释）。
	/// </summary>
	public string DisplayName { get; }

	/// <summary>
	/// 指标类型显示名。
	/// </summary>
	public string TypeDisplayName { get; }

	/// <summary>
	/// 所属类型全名。
	/// </summary>
	public string OwnerTypeFullName { get; }

	/// <summary>
	/// 创建指标节点。
	/// </summary>
	public FlowToolMetricNode(string nodeId, string metricName, string displayName, string typeDisplayName, string ownerTypeFullName)
	{
		NodeId = nodeId;
		MetricName = metricName;
		DisplayName = displayName;
		TypeDisplayName = typeDisplayName;
		OwnerTypeFullName = ownerTypeFullName;
	}
}

/// <summary>
/// flowtool 有向边定义。
/// </summary>
public sealed class FlowToolEdge
{
	/// <summary>
	/// 起点节点 ID。
	/// </summary>
	public string FromNodeId { get; }

	/// <summary>
	/// 终点节点 ID。
	/// </summary>
	public string ToNodeId { get; }

	/// <summary>
	/// 创建有向边。
	/// </summary>
	public FlowToolEdge(string fromNodeId, string toNodeId)
	{
		FromNodeId = fromNodeId;
		ToNodeId = toNodeId;
	}
}