using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 建筑仓库：支持任意商品存放，受商品数量限制
/// </summary>
public class Warehouse : IInfo, IPersistence<Warehouse>
{
	// 按人口维度分桶的产品库存。
	private readonly Dictionary<int, Dictionary<ProductType.Enums, float>> amounts = new();
	// 当前仓库已用容量缓存。不用考虑舍入误差
	private float totalUsedVolume;

	/// <summary>
	/// 仓库总容量上限。
	/// </summary>
	public float TotalVolume { get; }

	/// <summary>
	/// 已使用容量。
	/// </summary>
	public float TotalUsedVolume => totalUsedVolume;

	/// <summary>
	/// 剩余可用容量。
	/// </summary>
	public float RemainingVolume => TotalVolume - TotalUsedVolume;

	/// <summary>
	/// 创建仓库。
	/// </summary>
	public Warehouse(float totalVolume)
	{
		Debug.Assert(totalVolume >= 0.0f, "仓库总容量必须非负");
		TotalVolume = totalVolume;
		totalUsedVolume = 0.0f;
	}


	/// <summary>
	/// 添加产品，必须不能超过总容量限制，否则断言失败
	/// </summary>
	public void AddProduct(ProductType.Enums type, float amount, int populationId)
	{
		Debug.Assert(amount >= 0.0f, "添加数量必须非负");
		Debug.Assert(TotalUsedVolume + amount <= TotalVolume, $"仓库容量不足: {TotalUsedVolume + amount} > {TotalVolume}");

		float currentAmount = GetAmount(type, populationId);
		float targetAmount = currentAmount + amount;
		Dictionary<ProductType.Enums, float> bucket = getOrCreatePopulationBucket(populationId);
		bucket[type] = targetAmount;

		totalUsedVolume += amount;
	}

	/// <summary>
	/// 消耗产品，库存必须足够，否则断言失败
	/// </summary>
	public void ConsumeProduct(ProductType.Enums type, float amount, int populationId)
	{
		Debug.Assert(amount >= 0.0f, "消耗数量必须非负");

		float currentAmount = GetAmount(type, populationId);
		Debug.Assert(amount <= currentAmount, "库存不足，无法消耗");
		Dictionary<ProductType.Enums, float> bucket = getOrCreatePopulationBucket(populationId);
		float targetAmount = currentAmount - amount;
		bucket[type] = targetAmount;

		totalUsedVolume -= amount;
		Debug.Assert(totalUsedVolume >= 0.0f, "仓库已用容量不能为负");
	}

	/// <summary>
	/// 获取某商品在指定人口维度下的当前库存。
	/// </summary>
	public float GetAmount(ProductType.Enums type, int populationId)
	{
		if (!amounts.TryGetValue(populationId, out Dictionary<ProductType.Enums, float>? bucket))
		{
			return 0.0f;
		}

		return bucket.TryGetValue(type, out float value) ? value : 0.0f;
	}

	/// <summary>
	/// 获取某商品在全部人口上的总库存。
	/// </summary>
	public float GetTotalAmount(ProductType.Enums type)
	{
		return amounts.Values
			.Sum(bucket => bucket.TryGetValue(type, out float value) ? value : 0.0f);
	}




	/// <summary>
	/// 获取用于 UI 展示的字典数据。
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData data = new();
		amounts
			.SelectMany(popEntry => popEntry.Value.Select(productEntry => (populationId: popEntry.Key, productEntry.Key, productEntry.Value)))
			.Where(entry => entry.Value > 0.0f)
			.ToList()
			.ForEach(entry =>
			{
				ProductTemplate template = ProductTemplate.GetTemplate(entry.Key);
				float ratio = TotalVolume > 0.0f ? Mathf.Clamp(entry.Value / TotalVolume, 0.0f, 1.0f) : 0.0f;
				string populationName = $"人口#{entry.populationId}";
				data.AddProgress($"{populationName}/{template.Name}", ratio, $"{(int)entry.Value}");
			});

		Debug.Assert(TotalVolume >= 0.0f, "仓库总容量必须非负");
		float usedRatio = TotalVolume > 0.0f ? Mathf.Clamp(TotalUsedVolume / TotalVolume, 0.0f, 1.0f) : 0.0f;
		data.AddProgress("总仓容", usedRatio, $"{TotalUsedVolume:0.0} / {TotalVolume:0.0}");
		return data;
	}

	/// <summary>
	/// 获取仓库的持久化数据。
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		Dictionary<string, object> productAmounts = amounts
			.Where(popEntry => popEntry.Value.Any(productEntry => productEntry.Value > 0.0f))
			.ToDictionary(
				popEntry => popEntry.Key.ToString(),
				popEntry => (object)popEntry.Value
					.Where(productEntry => productEntry.Value > 0.0f)
					.ToDictionary(
						productEntry => ((int)productEntry.Key).ToString(),
						productEntry => (object)productEntry.Value));

		return new Dictionary<string, object>
		{
			{ "total_volume", TotalVolume },
			{ "amounts", productAmounts }
		};
	}

	/// <summary>
	/// 从持久化数据中加载仓库。
	/// </summary>
	public static Warehouse LoadSaveData(Dictionary<string, object> data)
	{
		float totalVolume = Convert.ToSingle(data["total_volume"]);
		Warehouse warehouse = new Warehouse(totalVolume);

		Dictionary<string, object> populationEntries = (data["amounts"] as Dictionary<string, object>)!;
		populationEntries
						.ToList()
						.ForEach(populationEntry =>
						{
							int populationId = Convert.ToInt32(populationEntry.Key);
							Dictionary<string, object> productEntries = (populationEntry.Value as Dictionary<string, object>)!;
							Dictionary<ProductType.Enums, float> bucket = productEntries
								.ToDictionary(
									productEntry => (ProductType.Enums)Convert.ToInt32(productEntry.Key),
									productEntry => Convert.ToSingle(productEntry.Value));
							warehouse.amounts[populationId] = bucket;
						});

		warehouse.totalUsedVolume = warehouse.amounts.Values
			.SelectMany(bucket => bucket.Values)
			.Sum();

		return warehouse;
	}

	// 获取人口库存桶，不存在时自动创建。
	private Dictionary<ProductType.Enums, float> getOrCreatePopulationBucket(int populationId)
	{
		if (amounts.TryGetValue(populationId, out Dictionary<ProductType.Enums, float>? bucket))
		{
			return bucket;
		}

		Dictionary<ProductType.Enums, float> created = new Dictionary<ProductType.Enums, float>();
		amounts[populationId] = created;
		return created;
	}
}