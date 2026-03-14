using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 类对应的作用域，管理这个域下的所有指标和关系
/// </summary>
public sealed class MetricScope
{
	private List<MetricRelation> metricRelations = new List<MetricRelation>();

	/// <summary>
	/// 名称，唯一标识符
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 作用域显示名。
	/// </summary>
	public string DisplayName { get; }

	/// <summary>
	/// 作用域内指标集合。
	/// </summary>
	public IReadOnlyDictionary<string, Metric> Metrics { get; }

	/// <summary>
	/// 作用域内指标的关系集合
	/// </summary>
	public IReadOnlyList<MetricRelation> MetricRelations => metricRelations;

	/// <summary>
	/// 创建作用域。
	/// </summary>
	public MetricScope(string name, string displayName, IEnumerable<Metric> metrics, IEnumerable<(string input, string output)> rawMetricRelations)
	{
		Name = name;
		DisplayName = displayName;
		Metrics = metrics.ToDictionary(m => m.Name, StringComparer.Ordinal);

		// 如果不存在对应的指标则忽略
		foreach (var relation in rawMetricRelations)
		{
			if (Metrics.ContainsKey(relation.input) && Metrics.ContainsKey(relation.output))
			{
				metricRelations.Add(new MetricRelation(Metrics[relation.input], Metrics[relation.output]));
			}
		}
	}
}