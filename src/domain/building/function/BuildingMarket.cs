using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 市场
/// </summary>
[Persistable]
public class BuildingMarket : IInfo
{
	private const string initialDt = "INIT";

	[PersistField]
	private ProductMarket productMarket = default!;

	[PersistField]
	private LabourMarket labourMarket = default!;

	/// <summary>
	/// 建筑内产品市场实例
	/// </summary>
	public ProductMarket ProductMarket => productMarket;

	/// <summary>
	/// 建筑内劳动力市场实例
	/// </summary>
	public LabourMarket LabourMarket => labourMarket;


	public BuildingMarket()
	{
		productMarket = new ProductMarket();
		labourMarket = new LabourMarket();
	}

	/// <summary>
	/// 获取市场模块的展示信息。
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData summary = new();
		summary.AddNumber("产品细分市场", ProductMarket.ItemTypes.Count);
		summary.AddNumber("职业细分市场", LabourMarket.ItemTypes.Count);
		summary.AddNumber("产品总需求", Math.Round(ProductMarket.ItemTypes.Sum(ProductMarket.GetDemand), 2));
		summary.AddNumber("产品总供给", Math.Round(ProductMarket.ItemTypes.Sum(ProductMarket.GetSupply), 2));
		summary.AddNumber("劳动力总需求", Math.Round(LabourMarket.ItemTypes.Sum(LabourMarket.GetDemand), 2));
		summary.AddNumber("劳动力总供给", Math.Round(LabourMarket.ItemTypes.Sum(LabourMarket.GetSupply), 2));

		InfoData data = new();
		data.AddGroup("市场概览", summary);
		data.AddGroup("产品价格", buildPriceInfo(ProductMarket));
		data.AddGroup("劳动力价格", buildPriceInfo(LabourMarket));
		return data;
	}

	// 构建市场价格展示分组。
	private InfoData buildPriceInfo<TItem>(Market<TItem> market) where TItem : struct, Enum
	{
		InfoData data = new();
		market.ItemTypes
			.ToList()
			.ForEach(itemType =>
			{
				float currentPrice = market.GetPrice(itemType);
				int historyCount = market.PriceHistory.Snapshots.Count;
				string historyLabel = historyCount <= 0 ? initialDt : market.PriceHistory.Snapshots.Last().Dt;
				data.AddText(itemType.ToString(), $"{currentPrice:0.00} (快照:{historyLabel})");
			});
		return data;
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
