using System;
using System.Collections.Generic;

/// <summary>
/// 买盘订单簿侧。
/// </summary>
[Persistable]
public sealed class BuySide : Side
{
    /// <summary>
    /// 初始化买盘排序规则：价格高优先，同价时间早优先。
    /// </summary>
    protected override SortedSet<Order> Init()
    {
        return new SortedSet<Order>(Comparer<Order>.Create(
            (p1, p2) =>
            {
                // 买盘价格高优先。
                if (p1.Price > p2.Price) { return -1; }
                if (p1.Price < p2.Price) { return 1; }

                // 同价时时间早优先。
                if (p1.Timestamp > p2.Timestamp) { return 1; }
                if (p1.Timestamp < p2.Timestamp) { return -1; }

                return 0;
            }));
    }
}

