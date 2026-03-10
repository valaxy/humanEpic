using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 财产，保存人口按产品类型的资产数量。
/// </summary>
[Persistable]
public class Asset : IInfo
{
	// 产品数量映射。
	[PersistField]
	private Dictionary<ProductType.Enums, float> amounts = default!;


	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private Asset()
	{
	}

	/// <summary>
	/// 初始化资产模块。
	/// </summary>
	public Asset(Dictionary<ProductType.Enums, float> amounts)
	{
		this.amounts = amounts;
	}

	/// <summary>
	/// 读取指定产品资产数量。
	/// </summary>
	public float GetAmount(ProductType.Enums type)
	{
		return amounts.ContainsKey(type) ? amounts[type] : 0.0f;
	}


	/// <summary>
	/// 设置指定产品数量。
	/// </summary>
	public void SetAmount(ProductType.Enums type, float amount)
	{
		Debug.Assert(amount >= 0.0f, "资产数量不能为负数");
		amounts[type] = amount;
	}

	/// <summary>
	/// 增加指定产品数量。
	/// </summary>
	public void AddAmount(ProductType.Enums type, float amount)
	{
		Debug.Assert(amount > 0.0f, "增加资产数量不能为负数");
		SetAmount(type, GetAmount(type) + amount);
	}

	/// <summary>
	/// 减少指定产品数量。
	/// </summary>
	public void ConsumeAmount(ProductType.Enums type, float amount)
	{
		Debug.Assert(amount > 0.0f, "消耗资产数量不能为负数");
		float currentAmount = GetAmount(type);
		Debug.Assert(currentAmount >= amount, "资产数量不足，无法消耗");
		SetAmount(type, currentAmount - amount);
	}


	/// <summary>
	/// 获取用于 UI 展示的资产数据。
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData data = new InfoData();

		amounts
			.OrderBy(item => item.Key)
			.Select(item => (name: ProductTemplate.GetTemplate(item.Key).Name, amount: item.Value))
			.ToList()
			.ForEach(item => data.AddNumber(item.name, item.amount));

		if (data.IsEmpty)
		{
			data.AddText("资产", "暂无");
		}

		return data;
	}
}