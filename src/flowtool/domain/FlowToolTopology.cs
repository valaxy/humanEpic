using System.Collections.Generic;

/// <summary>
/// flowtool 拓扑快照。
/// </summary>
public sealed record FlowToolTopology(
	IReadOnlyList<FlowToolMetricNode> Metrics,
	IReadOnlyList<FlowToolEdge> Edges
);

/// <summary>
/// flowtool 指标节点定义。
/// </summary>
public sealed record FlowToolMetricNode(
	string NodeId,
	string MetricName,
	string DisplayName,
	string TypeDisplayName,
	string OwnerTypeFullName
);

/// <summary>
/// flowtool 有向边定义。
/// </summary>
public sealed record FlowToolEdge(
	string FromNodeId,
	string ToNodeId
);