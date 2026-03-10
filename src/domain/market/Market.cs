using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 市场聚合基类，负责汇总多个商品或职业细分市场。
/// </summary>
/// <typeparam name="TItem">细分市场键类型（如商品类型、职业类型）。</typeparam>
public abstract class Market<TItem> where TItem : struct, Enum
{
    // 细分市场集合。
    private readonly IReadOnlyDictionary<TItem, ItemMarket> itemMarkets;
    private readonly PriceHistory<TItem> priceHistory = new();

    /// <summary>
    /// 历史价格
    /// </summary>
    public PriceHistory<TItem> PriceHistory => priceHistory;

    /// <summary>
    /// 初始化市场聚合器。
    /// </summary>
    /// <param name="itemTypes">细分市场键集合。</param>
    protected Market(IEnumerable<TItem> itemTypes)
    {
        itemMarkets = itemTypes
            .Distinct()
            .ToDictionary(itemType => itemType, itemType => new ItemMarket());
    }

    /// <summary>
    /// 新增买单。
    /// </summary>
    public void PlaceBuyOrder(TItem itemType, float price, int quantity, int agentId)
    {
        itemMarkets[itemType].PlaceBuyOrder(price, quantity, agentId);
    }

    /// <summary>
    /// 新增卖单。
    /// </summary>
    public void PlaceSellOrder(TItem itemType, float price, int quantity, int agentId)
    {
        itemMarkets[itemType].PlaceSellOrder(price, quantity, agentId);
    }

    /// <summary>
    /// 删除订单。
    /// </summary>
    public void ClearAgentOrders(IAgent agent)
    {
        itemMarkets.Values.ToList().ForEach(itemMarket => itemMarket.ClearAgentOrders(agent));
    }
}