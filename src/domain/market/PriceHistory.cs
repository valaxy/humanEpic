using System;
using System.Collections.Generic;
using System.Linq;



/// <summary>
/// 通用价格历史，支持按时间戳覆盖与固定容量截断。
/// </summary>
[Persistable]
public class PriceHistory<TEnum> where TEnum : struct, Enum
{
	private const int maxHistoryCount = 30;

	[PersistField]
	private List<PriceHistorySnapshot<TEnum>> snapshots = new();

	/// <summary>
	/// 当前历史快照（按时间先后）。
	/// </summary>
	public IReadOnlyList<PriceHistorySnapshot<TEnum>> Snapshots => snapshots;

	/// <summary>
	/// 记录一个价格快照；若时间戳重复则覆盖旧值。
	/// </summary>
	public void Record(string dt, IReadOnlyDictionary<TEnum, float> prices)
	{
		int existedIndex = snapshots.FindIndex(snapshot => snapshot.Dt == dt);
		if (existedIndex >= 0)
		{
			snapshots.RemoveAt(existedIndex);
		}

		Dictionary<TEnum, float> copiedPrices = prices.ToDictionary(item => item.Key, item => item.Value);
		snapshots.Add(new PriceHistorySnapshot<TEnum>(dt, copiedPrices));
		trimHistoryToLimit();
	}

	/// <summary>
	/// 清空历史后写入首条快照。
	/// </summary>
	public void ResetWithSingleSnapshot(string dt, IReadOnlyDictionary<TEnum, float> prices)
	{
		snapshots.Clear();
		Record(dt, prices);
	}

	private void trimHistoryToLimit()
	{
		int overflowCount = Math.Max(0, snapshots.Count - maxHistoryCount);
		if (overflowCount <= 0)
		{
			return;
		}

		snapshots.RemoveRange(0, overflowCount);
	}
}
