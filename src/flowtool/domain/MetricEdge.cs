/// <summary>
/// flowtool 有向边定义。
/// </summary>
public sealed class MetricEdge
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
	public MetricEdge(string fromNodeId, string toNodeId)
	{
		FromNodeId = fromNodeId;
		ToNodeId = toNodeId;
	}
}
