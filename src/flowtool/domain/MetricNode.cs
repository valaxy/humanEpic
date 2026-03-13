using System;

/// <summary>
/// flowtool 指标节点定义。
/// </summary>
public sealed class MetricNode
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
	public MetricNode(string nodeId, string metricName, string displayName, string typeDisplayName, string ownerTypeFullName)
	{
		NodeId = nodeId;
		MetricName = metricName;
		DisplayName = displayName;
		TypeDisplayName = typeDisplayName;
		OwnerTypeFullName = ownerTypeFullName;
	}

	/// <summary>
	/// 创建节点详情文本。
	/// </summary>
	public string CreateDetailText()
	{
		string displayLine = string.Equals(DisplayName, MetricName, StringComparison.Ordinal)
			? string.Empty
			: $"  显示名: {DisplayName}";
		return $"类型: {TypeDisplayName}{displayLine}";
	}
}
