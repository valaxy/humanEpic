using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 财产模块，保存人口按产品类型的资产数量。
/// </summary>
public class Asset : IPersistence<Asset>
{
	// 默认资产总量上限。
	private const float defaultMaxTotalAmount = 1000000.0f;

	// 产品数量映射。
	private readonly Dictionary<ProductType.Enums, float> amounts;

	/// <summary>
	/// 资产总量上限。
	/// </summary>
	public float MaxTotalAmount { get; }

	/// <summary>
	/// 当前资产总量。
	/// </summary>
	public float TotalAmount => amounts.Values.Sum();

	/// <summary>
	/// 初始化资产模块。
	/// </summary>
	public Asset(float maxTotalAmount = defaultMaxTotalAmount)
	{
		Debug.Assert(maxTotalAmount > 0.0f, "资产总量上限不能为负数");
		MaxTotalAmount = maxTotalAmount;
		amounts = Enum
			.GetValues<ProductType.Enums>()
			.ToDictionary(type => type, _ => 0.0f);
	}

	/// <summary>
	/// 读取指定产品资产数量。
	/// </summary>
	public float GetAmount(ProductType.Enums type)
	{
		return amounts[type];
	}


	/// <summary>
	/// 设置指定产品数量。
	/// </summary>
	public void SetAmount(ProductType.Enums type, float amount)
	{
		Debug.Assert(amount >= 0.0f, "资产数量不能为负数");
		float nextTotal = TotalAmount - amounts[type] + amount;
		Debug.Assert(nextTotal <= MaxTotalAmount, "资产总量不能超过上限");
		amounts[type] = amount;
	}

	/// <summary>
	/// 增加指定产品数量。
	/// </summary>
	public void AddAmount(ProductType.Enums type, float amount)
	{
		Debug.Assert(amount >= 0.0f, "增加资产数量不能为负数");
		SetAmount(type, amounts[type] + amount);
	}

	/// <summary>
	/// 减少指定产品数量。
	/// </summary>
	public void ConsumeAmount(ProductType.Enums type, float amount)
	{
		Debug.Assert(amount >= 0.0f, "消耗资产数量不能为负数");
		Debug.Assert(amounts[type] >= amount, "资产数量不足，无法消耗");
		SetAmount(type, amounts[type] - amount);
	}



	/// <summary>
	/// 获取保存数据。
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		Dictionary<string, object> data = new Dictionary<string, object>
		{
			{ "max_total_amount", MaxTotalAmount }
		};

		data["amounts"] = amounts.ToDictionary(item => item.Key.ToString(), item => (object)item.Value);
		return data;
	}

	/// <summary>
	/// 从保存数据恢复资产模块。
	/// </summary>
	public static Asset LoadSaveData(Dictionary<string, object> data)
	{
		float maxTotalAmount = data.ContainsKey("max_total_amount")
			? Convert.ToSingle(data["max_total_amount"])
			: defaultMaxTotalAmount;
		Asset asset = new Asset(maxTotalAmount);

		if (!data.ContainsKey("amounts"))
		{
			return asset;
		}

		Dictionary<string, object> amountData = (Dictionary<string, object>)data["amounts"];
		amountData
			.Where(item => Enum.TryParse(item.Key, true, out ProductType.Enums _))
			.ToList()
			.ForEach(item =>
			{
				Enum.TryParse(item.Key, true, out ProductType.Enums type);
				asset.SetAmount(type, Convert.ToSingle(item.Value));
			});

		return asset;
	}
}