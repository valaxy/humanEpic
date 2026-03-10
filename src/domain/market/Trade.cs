/// <summary>
/// 撮合成交记录。
/// </summary>
[Persistable]
public sealed class Trade
{
    [PersistField] private int buyAgent;
    [PersistField] private int sellAgent;
    [PersistField] private float price;
    [PersistField] private int quantity;


    /// <summary>
    /// 买方代理。
    /// </summary>
    public int BuyAgent => buyAgent;

    /// <summary>
    /// 卖方代理。
    /// </summary>
    public int SellAgent => sellAgent;

    /// <summary>
    /// 成交价。
    /// </summary>
    public float Price => price;

    /// <summary>
    /// 成交量。
    /// </summary>
    public int Quantity => quantity;

    /// <summary>
    /// 初始化成交记录。
    /// </summary>
    public Trade(
        int buyAgent,
        int sellAgent,
        float price,
        int quantity)
    {
        this.buyAgent = buyAgent;
        this.sellAgent = sellAgent;
        this.price = price;
        this.quantity = quantity;
    }

    /// <summary>
    /// 无参构造函数，供反持久化调用。
    /// </summary>
    private Trade() { }
}
