using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 市场聚合基类，负责汇总多个商品或职业细分市场。
/// </summary>
/// <typeparam name="TItem">细分市场键类型（如商品类型、职业类型）。</typeparam>
public abstract class Market<TItem> where TItem : notnull
{
    // 细分市场集合。
    private readonly IReadOnlyDictionary<TItem, ItemMarket> itemMarkets;

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
    public void PlaceBuyOrder(TItem itemType, float price, int quantity, IAgent agent)
    {
        itemMarkets[itemType].PlaceBuyOrder(price, quantity, agent);
    }

    /// <summary>
    /// 新增卖单。
    /// </summary>
    public void PlaceSellOrder(TItem itemType, float price, int quantity, IAgent agent)
    {
        itemMarkets[itemType].PlaceSellOrder(price, quantity, agent);
    }

    /// <summary>
    /// 删除订单。
    /// </summary>
    public void ClearAgentOrders(IAgent agent)
    {
        itemMarkets.Values.ToList().ForEach(itemMarket => itemMarket.ClearAgentOrders(agent));
    }
}