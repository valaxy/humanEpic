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
				snapshots.Select(snapshot => snapshot.Prices.ContainsKey(productType) ? snapshot.Prices[productType] : 0.0f).ToList()))
			.ToList();

		DataSource source = DataSource.CreateLineChart("产品价格历史（dt）", xLabels, seriesList);
		Render(source);
	}
}
