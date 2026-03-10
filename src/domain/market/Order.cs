using System.Diagnostics;

/// <summary>
/// 订单实体，在市场中记录当前订单状态
/// </summary>
[Persistable]
public sealed class Order
{
    [PersistField]
    private static long nextTimestamp = 1;

    [PersistField]
    private long timestamp;

    [PersistField]
    private int agentId;

    [PersistField]
    private float price;

    [PersistField]
    private int quantity;

    /// <summary>
    /// 订单时间戳，用于同价位先后顺序。
    /// </summary>
    public long Timestamp => timestamp;

    /// <summary>
    /// 订单所属代理。
    /// </summary>
    public int AgentId => agentId;

    /// <summary>
    /// 报价。
    /// </summary>
    public float Price => price;

    /// <summary>
    /// 剩余数量。
    /// </summary>
    public int Quantity => quantity;


    /// <summary>
    /// 初始化订单。
    /// </summary>
    public Order(int agentId, float price, int quantity)
    {
        Debug.Assert(price > 0.0f, "Order price must be greater than 0.");
        Debug.Assert(quantity > 0, "Order quantity must be greater than 0.");

        this.timestamp = nextTimestamp++;
        this.agentId = agentId;
        this.price = price;
        this.quantity = quantity;
    }

    /// <summary>
    /// 从存档回填订单。
    /// </summary>
    private Order() { }

    /// <summary>
    /// 扣减已成交数量。
    /// </summary>
    public void Fill(int quantity)
    {
        Debug.Assert(quantity > 0, "Fill quantity must be greater than 0.");
        Debug.Assert(quantity <= Quantity, "Fill quantity cannot exceed remaining quantity.");
        this.quantity -= quantity;
    }
}
