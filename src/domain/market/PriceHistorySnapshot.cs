using System;
using System.Collections.Generic;

/// <summary>
/// 价格历史快照。
/// </summary>
[Persistable]
public class PriceHistorySnapshot<TEnum> where TEnum : struct, Enum
{
    [PersistField]
    private string dt = default!;

    [PersistField]
    private Dictionary<TEnum, float> prices = default!;

    /// <summary>
    /// 时间戳。
    /// </summary>
    public string Dt => dt;

    /// <summary>
    /// 对应时间戳的价格快照。
    /// </summary>
    public IReadOnlyDictionary<TEnum, float> Prices => prices;

    /// <summary>
    /// 无参构造函数，供反持久化调用。
    /// </summary>
    private PriceHistorySnapshot() { }

    /// <summary>
    /// 初始化价格历史快照。
    public PriceHistorySnapshot(string dt, Dictionary<TEnum, float> prices)
    {
        this.dt = dt;
        this.prices = prices;
    }
}