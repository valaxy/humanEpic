using System;
using System.Diagnostics;

/// <summary>
/// 订单实体，在市场中记录当前订单状态
/// </summary>
public sealed class Order
{
    private static long nextTimestamp = 1;

    /// <summary>
    /// 订单时间戳，用于同价位先后顺序。
    /// </summary>
    public long Timestamp { get; } = nextTimestamp++;

    /// <summary>
    /// 订单所属代理。
    /// </summary>
    public IAgent Agent { get; }

    /// <summary>
    /// 报价。
    /// </summary>
    public float Price { get; }

    /// <summary>
    /// 剩余数量。
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// 初始化订单。
    /// </summary>
    public Order(IAgent agent, float price, int quantity)
    {
        Debug.Assert(price > 0.0f, "Order price must be greater than 0.");
        Debug.Assert(quantity > 0, "Order quantity must be greater than 0.");

        Agent = agent;
        Price = price;
        Quantity = quantity;
    }

    /// <summary>
    /// 扣减已成交数量。
    /// </summary>
    public void Fill(int quantity)
    {
        Debug.Assert(quantity > 0, "Fill quantity must be greater than 0.");
        Debug.Assert(quantity <= Quantity, "Fill quantity cannot exceed remaining quantity.");
        Quantity -= quantity;
    }
}
