using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 市场
/// </summary>
public class Market : IInfo, IPersistence<Market>
{
	private const string initialDt = "INIT";

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
		marketInfo.AddGroup("商品价格历史", buildProductPriceHistoryInfo());
		marketInfo.AddGroup("劳动力价格历史", buildLabourPriceHistoryInfo());

		return marketInfo;
	}

	/// <summary>
	/// 获取市场模块持久化数据。
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		return new Dictionary<string, object>
		{
			{ "product_consumer_demands", ProductMarket.ConsumerDemands.GetSaveData() },
			{ "product_industry_demands", ProductMarket.IndustryDemands.GetSaveData() },
			{ "product_supplies", ProductMarket.Supplies.GetSaveData() },
			{ "product_prices", ProductMarket.Prices.GetSaveData() },
			{ "product_price_history", ProductMarket.PriceHistory.GetSaveData() },
			{ "labour_prices", LabourMarket.JobPrices.GetSaveData() },
			{ "labour_supplies", LabourMarket.JobSupplies.GetSaveData() },
			{ "labour_demands", LabourMarket.JobDemands.GetSaveData() },
			{ "labour_price_history", LabourMarket.PriceHistory.GetSaveData() }
		};
	}

	/// <summary>
	/// 从持久化数据恢复市场模块。
	/// </summary>
	public static Market LoadSaveData(Dictionary<string, object> data)
	{
		Market market = new();

		applySavedBucket(data, "product_consumer_demands", market.ProductMarket.ConsumerDemands);
		applySavedBucket(data, "product_industry_demands", market.ProductMarket.IndustryDemands);
		applySavedBucket(data, "product_supplies", market.ProductMarket.Supplies);
		applySavedBucket(data, "product_prices", market.ProductMarket.Prices);
		applySavedBucket(data, "labour_prices", market.LabourMarket.JobPrices);
		applySavedBucket(data, "labour_supplies", market.LabourMarket.JobSupplies);
		applySavedBucket(data, "labour_demands", market.LabourMarket.JobDemands);
		loadHistory(data, "product_price_history", historyData => market.ProductMarket.LoadPriceHistory(historyData), () => market.ProductMarket.ResetPriceHistory(initialDt));
		loadHistory(data, "labour_price_history", historyData => market.LabourMarket.LoadPriceHistory(historyData), () => market.LabourMarket.ResetPriceHistory(initialDt));

		return market;
	}

	// 将存档字段回填到目标市场数据桶。
	private static void applySavedBucket<TEnum>(Dictionary<string, object> data, string fieldName, MarketDataBucket<TEnum> target) where TEnum : struct, Enum
	{
		if (!data.ContainsKey(fieldName))
		{
			return;
		}

		if (data[fieldName] is not Dictionary<string, object> savedBucket)
		{
			return;
		}

		target.ApplySaveData(savedBucket);
	}

	// 构造商品价格历史信息。
	private InfoData buildProductPriceHistoryInfo()
	{
		InfoData historyInfo = new();
		ProductMarket.PriceHistory.Snapshots
			.Reverse()
			.ToList()
			.ForEach(snapshot =>
			{
				InfoData snapshotInfo = new();
				snapshot.Prices
					.OrderBy(item => item.Key)
					.ToList()
					.ForEach(priceEntry => snapshotInfo.AddText(ProductTemplate.GetTemplate(priceEntry.Key).Name, priceEntry.Value.ToString("0.00")));
				historyInfo.AddGroup(snapshot.Dt, snapshotInfo);
			});

		if (historyInfo.IsEmpty)
		{
			historyInfo.AddText("状态", "无数据");
		}

		return historyInfo;
	}

	// 构造劳动力价格历史信息。
	private InfoData buildLabourPriceHistoryInfo()
	{
		InfoData historyInfo = new();
		LabourMarket.PriceHistory.Snapshots
			.Reverse()
			.ToList()
			.ForEach(snapshot =>
			{
				InfoData snapshotInfo = new();
				snapshot.Prices
					.OrderBy(item => item.Key)
					.ToList()
					.ForEach(priceEntry => snapshotInfo.AddText(JobTemplate.GetTemplate(priceEntry.Key).Name, priceEntry.Value.ToString("0.00")));
				historyInfo.AddGroup(snapshot.Dt, snapshotInfo);
			});

		if (historyInfo.IsEmpty)
		{
			historyInfo.AddText("状态", "无数据");
		}

		return historyInfo;
	}

	// 统一处理价格历史读取与旧存档兼容兜底。
	private static void loadHistory(Dictionary<string, object> data, string fieldName, Action<List<Dictionary<string, object>>> onLoaded, Action onMissing)
	{
		if (!data.ContainsKey(fieldName))
		{
			onMissing();
			return;
		}

		if (data[fieldName] is not List<object> rawHistory)
		{
			onMissing();
			return;
		}

		List<Dictionary<string, object>> historyData = rawHistory
			.Select(item => (Dictionary<string, object>)item)
			.ToList();
		onLoaded(historyData);
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
