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

	/// <summary>
	/// 导出历史存档。
	/// </summary>
	public List<Dictionary<string, object>> GetSaveData()
	{
		return snapshots
			.Select(snapshot => new Dictionary<string, object>
			{
				{ "dt", snapshot.Dt },
				{ "prices", snapshot.Prices.ToDictionary(item => item.Key.ToString(), item => (object)item.Value) }
			})
			.ToList();
	}

	/// <summary>
	/// 从存档回填历史。
	/// </summary>
	public void LoadSaveData(List<Dictionary<string, object>> data)
	{
		snapshots.Clear();
		data.ForEach(item =>
		{
			string dt = item["dt"].ToString() ?? string.Empty;
			Dictionary<string, object> rawPrices = (Dictionary<string, object>)item["prices"];
			Dictionary<TEnum, float> parsedPrices = rawPrices
				.Where(priceEntry => Enum.TryParse(priceEntry.Key, true, out TEnum _))
				.Select(priceEntry =>
				{
					Enum.TryParse(priceEntry.Key, true, out TEnum parsedType);
					return (parsedType, Price: Convert.ToSingle(priceEntry.Value));
				})
				.ToDictionary(item => item.parsedType, item => item.Price);

			Record(dt, parsedPrices);
		});
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
