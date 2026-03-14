using Godot;


public sealed class TopologyNode
{
    private Metric metric;

    /// <summary>
    /// 节点 ID，唯一标识符。
    /// </summary>
    public string Id => metric.Name;

    /// <summary>
    /// 布局坐标
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// 是否激活，未激活的节点不参与渲染和交互
    /// </summary>
    public bool IsActive { get; set; } = false;


    /// <summary>
    /// 底层数据暴露出来
    /// </summary>
    public Metric Metric => metric;


    /// <summary>
    /// 基于Metric包装
    /// </summary>
    public TopologyNode(Metric metric)
    {
        this.metric = metric;
    }
}