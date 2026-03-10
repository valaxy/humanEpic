using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 通用市场数据桶，提供按类型读取、累加、重置与覆写能力。
/// </summary>
/// <typeparam name="TKey">市场类型键（通常为枚举）。</typeparam>
public class MarketDataBucket<TKey> : IPersistence<MarketDataBucket<TKey>, IEnumerable<TKey>> where TKey : struct, Enum
{
	// 当前数据表。
	private readonly Dictionary<TKey, float> values;

	public MarketDataBucket(IEnumerable<TKey> keys, Func<TKey, float> initialValueFactory)
	{
		values = keys.ToDictionary(key => key, initialValueFactory);
	}

	/// <summary>
	/// 获取指定类型当前值。
	/// </summary>
	public float Get(TKey key)
	{
		return values[key];
	}

	/// <summary>
	/// 累加指定类型当前值，并返回累加后的结果。
	/// </summary>
	public float Add(TKey key, float amount)
	{
		float next = Get(key) + amount;
		values[key] = next;
		return next;
	}

	/// <summary>
	/// 覆写指定类型当前值。
	/// </summary>
	public void Set(TKey key, float amount)
	{
		values[key] = amount;
	}

	/// <summary>
	/// 将所有类型值重置为指定常量（默认 0）。
	/// </summary>
	public void Reset()
	{
		List<TKey> keys = values.Keys.ToList();
		keys.ForEach(key => values[key] = 0.0f);
	}

	/// <summary>
	/// 导出当前数据桶。
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		return values.ToDictionary(item => item.Key.ToString(), item => (object)item.Value);
	}

	/// <summary>
	/// 使用存档数据回填当前数据桶。
	/// </summary>
	public void ApplySaveData(Dictionary<string, object> data)
	{
		data
			.Where(item => Enum.TryParse(item.Key, true, out TKey _))
			.ToList()
			.ForEach(item =>
			{
				Enum.TryParse(item.Key, true, out TKey key);
				values[key] = Convert.ToSingle(item.Value);
			});
	}

	/// <summary>
	/// 从存档恢复一个新的数据桶。
	/// </summary>
	public static MarketDataBucket<TKey> LoadSaveData(Dictionary<string, object> data, IEnumerable<TKey>? context = default)
	{
		IEnumerable<TKey> keys = context ?? Enum.GetValues<TKey>();
		MarketDataBucket<TKey> bucket = new(keys, _ => 0.0f);
		bucket.ApplySaveData(data);
		return bucket;
	}
}