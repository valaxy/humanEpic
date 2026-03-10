using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 市场聚合基类，负责汇总多个商品或职业细分市场。
/// </summary>
/// <typeparam name="TItem">细分市场键类型（如商品类型、职业类型）。</typeparam>
[Persistable]
public abstract class Market<TItem> where TItem : struct, Enum
{
    // 细分市场集合。
    [PersistField]
    private Dictionary<TItem, ItemMarket> itemMarkets;

    [PersistField]
    private Dictionary<TItem, float> prices;

    [PersistField]
    private PriceHistory<TItem> priceHistory = new();

    /// <summary>
    /// 市场数据变化事件。
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// 当前全部细分市场键。
    /// </summary>
    public IReadOnlyList<TItem> ItemTypes => itemMarkets.Keys.ToList();

    /// <summary>
    /// 历史价格
    /// </summary>
    public PriceHistory<TItem> PriceHistory => priceHistory;

    /// <summary>
    /// 当前价格快照。
    /// </summary>
    public IReadOnlyDictionary<TItem, float> Prices => prices;

    /// <summary>
    /// 初始化市场聚合器。
    /// </summary>
    /// <param name="itemTypes">细分市场键集合。</param>
    protected Market(IEnumerable<TItem> itemTypes)
    {
        List<TItem> allTypes = itemTypes
            .Distinct()
            .ToList();

        itemMarkets = allTypes
            .ToDictionary(itemType => itemType, _ => new ItemMarket());

        prices = allTypes
            .ToDictionary(itemType => itemType, _ => 0.0f);
    }

    /// <summary>
    /// 新增买单。
    /// </summary>
    public void PlaceBuyOrder(TItem itemType, float price, int quantity, int agentId)
    {
        itemMarkets[itemType].PlaceBuyOrder(price, quantity, agentId);
        Changed?.Invoke();
    }

    /// <summary>
    /// 新增卖单。
    /// </summary>
    public void PlaceSellOrder(TItem itemType, float price, int quantity, int agentId)
    {
        itemMarkets[itemType].PlaceSellOrder(price, quantity, agentId);
        Changed?.Invoke();
    }

    /// <summary>
    /// 设置细分市场价格。
    /// </summary>
    public void SetPrice(TItem itemType, float price)
    {
        prices[itemType] = price;
        Changed?.Invoke();
    }

    /// <summary>
    /// 读取细分市场价格。
    /// </summary>
    public float GetPrice(TItem itemType)
    {
        return prices[itemType];
    }

    /// <summary>
    /// 读取细分市场需求量（买单总量）。
    /// </summary>
    public float GetDemand(TItem itemType)
    {
        return itemMarkets[itemType].BuyQuantity;
    }

    /// <summary>
    /// 读取细分市场供应量（卖单总量）。
    /// </summary>
    public float GetSupply(TItem itemType)
    {
        return itemMarkets[itemType].SellQuantity;
    }

    /// <summary>
    /// 读取细分市场买单簿快照。
    /// </summary>
    public IReadOnlyList<(float price, int quantity)> GetBuyOrders(TItem itemType)
    {
        return itemMarkets[itemType].GetBuyOrderBookSnapshot();
    }

    /// <summary>
    /// 读取细分市场卖单簿快照。
    /// </summary>
    public IReadOnlyList<(float price, int quantity)> GetSellOrders(TItem itemType)
    {
        return itemMarkets[itemType].GetSellOrderBookSnapshot();
    }

    /// <summary>
    /// 执行单个细分市场撮合。
    /// </summary>
    public IReadOnlyList<Trade> TradeAll(TItem itemType)
    {
        List<Trade> trades = itemMarkets[itemType].TradeAll();
        if (trades.Count > 0)
        {
            float weightedPrice = trades.Sum(trade => trade.Price * trade.Quantity) / Math.Max(1, trades.Sum(trade => trade.Quantity));
            SetPrice(itemType, weightedPrice);
        }

        Changed?.Invoke();
        return trades;
    }

    /// <summary>
    /// 记录当前价格快照。
    /// </summary>
    public void RecordPriceSnapshot(string dt)
    {
        priceHistory.Record(dt, prices);
        Changed?.Invoke();
    }

    /// <summary>
    /// 删除订单。
    /// </summary>
    public void ClearAgentOrders(IAgent agent)
    {
        itemMarkets.Values.ToList().ForEach(itemMarket => itemMarket.ClearAgentOrders(agent));
        Changed?.Invoke();
    }

    /// <summary>
    /// 手动触发市场更新事件。
    /// </summary>
    public void NotifyChanged()
    {
        Changed?.Invoke();
    }

    /// <summary>
    /// 通用
    /// </summary>
    private Market()
    {
        itemMarkets = new Dictionary<TItem, ItemMarket>();
        prices = new Dictionary<TItem, float>();
    }
}