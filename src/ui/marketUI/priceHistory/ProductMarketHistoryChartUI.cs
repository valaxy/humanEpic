using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 产品市场价格历史折线图组件。
/// </summary>
[GlobalClass]
public partial class ProductMarketHistoryChartUI : LineChart
{
	// 折线配色。
	private static readonly List<Color> chartColors =
	[
		Colors.CornflowerBlue,
		Colors.Orange,
		Colors.MediumSeaGreen,
		Colors.Gold,
		Colors.Tomato,
		Colors.MediumPurple,
		Colors.DeepSkyBlue,
		Colors.LightCoral
	];

	/// <summary>
	/// 根据产品市场历史数据刷新折线图。
	/// </summary>
	public void RenderMarket(ProductMarket market)
	{
		List<PriceHistorySnapshot<ProductType.Enums>> snapshots = market.PriceHistory.Snapshots.ToList();
		List<string> xLabels = snapshots.Select(snapshot => snapshot.Dt).ToList();
		List<DataSeries> seriesList = Enum.GetValues<ProductType.Enums>()
			.Select((productType, index) => DataSeries.Create(
				ProductTemplate.GetTemplate(productType).Name,
				chartColors[index % chartColors.Count].ToHtml(),
				snapshots.Select(snapshot => getSnapshotPrice(snapshot, productType)).ToList()))
			.ToList();

		DataSource source = DataSource.CreateLineChart("产品价格历史（dt）", xLabels, seriesList);
		Render(source);
	}

	/// <summary>
	/// 根据指定商品刷新单条历史折线图。
	/// </summary>
	public void RenderSingleProduct(ProductMarket market, ProductType.Enums productType)
	{
		List<PriceHistorySnapshot<ProductType.Enums>> snapshots = market.PriceHistory.Snapshots.ToList();
		List<string> xLabels = snapshots.Select(snapshot => snapshot.Dt).ToList();
		DataSeries series = DataSeries.Create(
			ProductTemplate.GetTemplate(productType).Name,
			chartColors[((int)productType) % chartColors.Count].ToHtml(),
			snapshots.Select(snapshot => getSnapshotPrice(snapshot, productType)).ToList());

		DataSource source = DataSource.CreateLineChart($"{ProductTemplate.GetTemplate(productType).Name}价格历史（dt）", xLabels, [series]);
		Render(source);
	}

	// 获取快照中某商品的价格。
	private static float getSnapshotPrice(PriceHistorySnapshot<ProductType.Enums> snapshot, ProductType.Enums productType)
	{
		return snapshot.Prices.TryGetValue(productType, out float price)
			? price
			: 0.0f;
	}
}
