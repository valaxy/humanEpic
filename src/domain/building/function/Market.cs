using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 市场
/// </summary>
public class Market : IInfo, IPersistence<Market>
{
	/// <summary>
	/// 建筑内产品市场实例
	/// </summary>
	public ProductMarket ProductMarket { get; }

	/// <summary>
	/// 建筑内劳动力市场实例
	/// </summary>
	public LabourMarket LabourMarket { get; }


	public Market()
	{
		ProductMarket = new ProductMarket();
		LabourMarket = new LabourMarket();
	}

	/// <summary>
	/// 获取市场模块的展示信息。
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData marketInfo = new();
		marketInfo.AddText("产品种类", Enum.GetValues<ProductType.Enums>().Length.ToString());
		marketInfo.AddText("职业种类", Enum.GetValues<JobType.Enums>().Length.ToString());

		float totalDemand = Enum.GetValues<ProductType.Enums>()
			.Select(type => ProductMarket.ConsumerDemands.Get(type) + ProductMarket.IndustryDemands.Get(type))
			.Sum();
		float totalSupply = Enum.GetValues<ProductType.Enums>()
			.Select(type => ProductMarket.Supplies.Get(type))
			.Sum();
		marketInfo.AddText("总需求", totalDemand.ToString("0.00"));
		marketInfo.AddText("总供给", totalSupply.ToString("0.00"));

		return marketInfo;
	}

	/// <summary>
	/// 获取市场模块持久化数据。
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		return new Dictionary<string, object>
		{
			{ "product_consumer_demands", serializeBucket(Enum.GetValues<ProductType.Enums>(), ProductMarket.ConsumerDemands.Get) },
			{ "product_industry_demands", serializeBucket(Enum.GetValues<ProductType.Enums>(), ProductMarket.IndustryDemands.Get) },
			{ "product_supplies", serializeBucket(Enum.GetValues<ProductType.Enums>(), ProductMarket.Supplies.Get) },
			{ "product_prices", serializeBucket(Enum.GetValues<ProductType.Enums>(), ProductMarket.Prices.Get) },
			{ "labour_prices", serializeBucket(Enum.GetValues<JobType.Enums>(), LabourMarket.JobPrices.Get) },
			{ "labour_supplies", serializeBucket(Enum.GetValues<JobType.Enums>(), LabourMarket.JobSupplies.Get) },
			{ "labour_demands", serializeBucket(Enum.GetValues<JobType.Enums>(), LabourMarket.JobDemands.Get) }
		};
	}

	/// <summary>
	/// 从持久化数据恢复市场模块。
	/// </summary>
	public static Market LoadSaveData(Dictionary<string, object> data)
	{
		Market market = new();

		deserializeBucket<ProductType.Enums>(data, "product_consumer_demands", market.ProductMarket.ConsumerDemands.Set);
		deserializeBucket<ProductType.Enums>(data, "product_industry_demands", market.ProductMarket.IndustryDemands.Set);
		deserializeBucket<ProductType.Enums>(data, "product_supplies", market.ProductMarket.Supplies.Set);
		deserializeBucket<ProductType.Enums>(data, "product_prices", market.ProductMarket.Prices.Set);
		deserializeBucket<JobType.Enums>(data, "labour_prices", market.LabourMarket.JobPrices.Set);
		deserializeBucket<JobType.Enums>(data, "labour_supplies", market.LabourMarket.JobSupplies.Set);
		deserializeBucket<JobType.Enums>(data, "labour_demands", market.LabourMarket.JobDemands.Set);

		return market;
	}

	// 序列化市场数据桶，键使用枚举名称。
	private static Dictionary<string, object> serializeBucket<TEnum>(IEnumerable<TEnum> keys, Func<TEnum, float> getValue) where TEnum : struct, Enum
	{
		return keys.ToDictionary(key => key.ToString(), key => (object)getValue(key));
	}

	// 反序列化市场数据桶并回填到目标 setter。
	private static void deserializeBucket<TEnum>(Dictionary<string, object> data, string fieldName, Action<TEnum, float> setValue) where TEnum : struct, Enum
	{
		if (!data.ContainsKey(fieldName))
		{
			return;
		}

		if (data[fieldName] is not Dictionary<string, object> savedBucket)
		{
			return;
		}

		savedBucket
			.Where(item => Enum.TryParse(item.Key, true, out TEnum _))
			.ToList()
			.ForEach(item =>
			{
				Enum.TryParse(item.Key, true, out TEnum key);
				float value = Convert.ToSingle(item.Value);
				setValue(key, value);
			});
	}




	// /// <summary>
	// /// 获取市场覆盖到的民宅建筑列表
	// /// </summary>
	// public List<ResidentialBuilding> GetCoveredResidentialBuildings(Ground ground, BuildingCollection buildingCollection)
	// {
	// 	List<ResidentialBuilding> result = new List<ResidentialBuilding>();
	// 	if (ground == null || buildingCollection == null)
	// 	{
	// 		return result;
	// 	}

	// 	Vector2 center = Collision.CenterAtGrid;
	// 	List<Vector2I> cells = Template.ServiceRange.GetCoveredCells(center, ground.Width, ground.Height);
	// 	HashSet<Vector2I> coveredCells = new(cells);

	// 	foreach (ResidentialBuilding residential in buildingCollection.GetBuildings<ResidentialBuilding>())
	// 	{
	// 		if (residential == null) continue;
	// 		if (isBuildingCoveredByCells(residential.Collision.Center, coveredCells))
	// 		{
	// 			result.Add(residential);
	// 		}
	// 	}
	// 	return result;
	// }


	// private bool isBuildingCoveredByCells(Vector2I buildingTopLeft, HashSet<Vector2I> coveredCells)
	// {
	// 	for (int dx = 0; dx < 2; dx++)
	// 	{
	// 		for (int dy = 0; dy < 2; dy++)
	// 		{
	// 			if (coveredCells.Contains(new Vector2I(buildingTopLeft.X + dx, buildingTopLeft.Y + dy)))
	// 			{
	// 				return true;
	// 			}
	// 		}
	// 	}
	// 	return false;
	// }
}
