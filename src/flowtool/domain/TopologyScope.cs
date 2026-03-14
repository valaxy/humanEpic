using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 类列表项，负责保存拓扑结构
/// </summary>
public sealed class TopologyScope
{
	/// <summary>
	/// 作用域键。
	/// </summary>
	public string ScopeKey { get; }

	/// <summary>
	/// 作用域显示名。
	/// </summary>
	public string DisplayName { get; }

	/// <summary>
	/// 作用域内指标节点。
	/// </summary>
	public IReadOnlyList<MetricNode> MetricNodes { get; }

	/// <summary>
	/// 作用域内连线。
	/// </summary>
	public IReadOnlyList<MetricEdge> MetricEdges { get; }

	/// <summary>
	/// 创建作用域。
	/// </summary>
	public TopologyScope(
		string scopeKey,
		string displayName,
		IReadOnlyList<MetricNode>? metricNodes = null,
		IReadOnlyList<MetricEdge>? metricEdges = null)
	{
		ScopeKey = scopeKey;
		DisplayName = displayName;
		MetricNodes = metricNodes is null
			? Array.Empty<MetricNode>()
			: metricNodes.ToList();
		MetricEdges = metricEdges is null
			? Array.Empty<MetricEdge>()
			: metricEdges.ToList();
	}
}