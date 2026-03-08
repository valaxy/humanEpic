using System;
using System.Collections.Generic;

/// <summary>
/// 卖盘订单簿侧。
/// </summary>
public sealed class SellSide : Side
{
    /// <summary>
    /// 初始化卖盘排序规则：价格低优先，同价时间早优先。
    /// </summary>
    protected override SortedSet<Order> Init()
    {
        return new SortedSet<Order>(Comparer<Order>.Create(
            (p1, p2) =>
            {
                // 卖盘价格低优先。
                if (p1.Price > p2.Price) { return 1; }
                if (p1.Price < p2.Price) { return -1; }

                // 同价时时间早优先。
                if (p1.Timestamp > p2.Timestamp) { return 1; }
                if (p1.Timestamp < p2.Timestamp) { return -1; }

                return 0;
            }));
    }
}

