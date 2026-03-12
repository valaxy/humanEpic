using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 财产，保存人口按产品类型的资产数量。
/// </summary>
[Persistable]
public class Asset : DictCollection<ProductType.Enums, AssetItem>, IInfo
{
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
		amounts
			.Select(item => new AssetItem(item.Key, item.Value))
			.ToList()
			.ForEach(Add);
	}

	/// <summary>
	/// 获取资产条目的键。
	/// </summary>
	protected override ProductType.Enums GetKey(AssetItem item) => item.ProductType;

	/// <summary>
	/// 读取指定产品资产数量。
	/// </summary>
	public float GetAmount(ProductType.Enums type)
	{
		return HasKey(type) ? Get(type).Amount : 0.0f;
	}


	/// <summary>
	/// 返回所有的产品键
	/// </summary>
	public IEnumerable<ProductType.Enums> GetProductTypes()
	{
		return GetAll().Select(item => item.ProductType);
	}


	/// <summary>
	/// 设置指定产品数量。
	/// </summary>
	public void SetAmount(ProductType.Enums type, float amount)
	{
		if (HasKey(type))
		{
			Get(type).SetAmount(amount);
			return;
		}

		Add(new AssetItem(type, amount));
	}

	/// <summary>
	/// 增加指定产品数量。
	/// </summary>
	public void AddAmount(ProductType.Enums type, float amount)
	{
		if (HasKey(type))
		{
			Get(type).AddAmount(amount);
			return;
		}

		Add(new AssetItem(type, amount));
	}

	/// <summary>
	/// 减少指定产品数量并记录消费天数。
	/// </summary>
	public void ConsumeAmount(ProductType.Enums type, float amount, int day)
	{
		Get(type).ConsumeAmount(amount, day);
	}


	/// <summary>
	/// 获取用于 UI 展示的资产数据。
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData data = new InfoData();

		GetAll()
			.OrderBy(item => item.ProductType)
			.Select(item => (name: item.Template.Name, amount: item.Amount))
			.ToList()
			.ForEach(item => data.AddNumber(item.name, item.amount));

		if (data.IsEmpty)
		{
			data.AddText("资产", "暂无");
		}

		return data;
	}
}