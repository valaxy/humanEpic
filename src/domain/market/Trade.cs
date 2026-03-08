/// <summary>
/// 撮合成交记录。
/// </summary>
public sealed class Trade
{
    /// <summary>
    /// 买方代理。
    /// </summary>
    public IAgent BuyAgent { get; }

    /// <summary>
    /// 卖方代理。
    /// </summary>
    public IAgent SellAgent { get; }

    /// <summary>
    /// 成交价。
    /// </summary>
    public float Price { get; }

    /// <summary>
    /// 成交量。
    /// </summary>
    public int Quantity { get; }

    /// <summary>
    /// 初始化成交记录。
    /// </summary>
    public Trade(
        IAgent buyAgent,
        IAgent sellAgent,
        float price,
        int quantity)
    {
        BuyAgent = buyAgent;
        SellAgent = sellAgent;
        Price = price;
        Quantity = quantity;
    }
}
