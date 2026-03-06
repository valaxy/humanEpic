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
	private readonly Dictionary<ProductType.Enums, float> amounts = new();

	/// <summary>
	/// 仓库总容量上限。
	/// </summary>
	public float TotalVolume { get; }

	/// <summary>
	/// 已使用容量。
	/// </summary>
	public float TotalUsedVolume => amounts.Sum(entry => entry.Value);

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
	}


	/// <summary>
	/// 添加产品，必须不能超过总容量限制，否则断言失败
	/// </summary>
	public void AddProduct(ProductType.Enums type, float amount)
	{
		Debug.Assert(amount >= 0.0f, "添加数量必须非负");
		Debug.Assert(TotalUsedVolume + amount <= TotalVolume, "仓库容量不足");

		float currentAmount = GetAmount(type);
		float targetAmount = currentAmount + amount;

		amounts[type] = targetAmount;
	}

	/// <summary>
	/// 消耗产品，库存必须足够，否则断言失败
	/// </summary>
	public void ConsumeProduct(ProductType.Enums type, float amount)
	{
		Debug.Assert(amount >= 0.0f, "消耗数量必须非负");

		float currentAmount = GetAmount(type);
		Debug.Assert(amount <= currentAmount, "库存不足，无法消耗");

		amounts[type] = currentAmount - amount;
	}

	/// <summary>
	/// 获取某商品当前库存。
	/// </summary>
	public float GetAmount(ProductType.Enums type)
	{
		return amounts.TryGetValue(type, out float value) ? value : 0.0f;
	}




	/// <summary>
	/// 获取用于 UI 展示的字典数据。
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData data = new();
		foreach (KeyValuePair<ProductType.Enums, float> pair in amounts.Where(entry => entry.Value > 0.0f))
		{
			ProductTemplate template = ProductTemplate.GetTemplate(pair.Key);
			float ratio = TotalVolume > 0.0f ? Mathf.Clamp((pair.Value) / TotalVolume, 0.0f, 1.0f) : 0.0f;
			data.AddProgress(template.Name, ratio, $"{(int)pair.Value}");
		}

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
		Dictionary<string, object> productAmounts = new();
		foreach (KeyValuePair<ProductType.Enums, float> pair in amounts)
		{
			if (pair.Value > 0.0f)
			{
				productAmounts[pair.Key.ToString()] = pair.Value;
			}
		}

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

		if (data.TryGetValue("amounts", out object? amountsObj) && amountsObj is Dictionary<string, object> amountsDict)
		{
			foreach (KeyValuePair<string, object> pair in amountsDict)
			{
				if (Enum.TryParse(pair.Key, out ProductType.Enums type))
				{
					warehouse.amounts[type] = Convert.ToSingle(pair.Value);
				}
			}
		}

		return warehouse;
	}
}