


public sealed class TopologyEdge
{
    /// <summary>
    /// 边的起始节点
    /// </summary>
    public TopologyNode FromNode { get; }

    /// <summary>
    /// 边的目标节点
    /// </summary>
    public TopologyNode ToNode { get; }

    /// <summary>
    /// 创建拓扑边。
    /// </summary>
    public TopologyEdge(TopologyNode fromNode, TopologyNode toNode)
    {
        FromNode = fromNode;
        ToNode = toNode;
    }
}