using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 人口过去 30 天商品消费折线图组件。
/// </summary>
[GlobalClass]
public partial class PopulationConsumptionHistoryChartUI : LineChartView
{
	// 折线配色。
	private static readonly List<Color> chartColors =
	[
		Colors.CornflowerBlue,
		Colors.Orange,
		Colors.MediumSeaGreen,
		Colors.Gold,
		Colors.Tomato,
		Colors.DeepSkyBlue,
		Colors.LightCoral,
		Colors.SlateBlue
	];

	/// <summary>
	/// 渲染指定人口的过去 30 天消费历史。
	/// </summary>
	public void RenderPopulation(Population population, int currentDay)
	{
		List<int> dayIndexes = Enumerable.Range(currentDay - 29, 30)
			.Select(day => Math.Max(day, 0))
			.ToList();

		List<AssetItem> consumerGoods = population.Asset.GetAll()
			.Where(item => item.Template.IsConsumerGood)
			.OrderBy(item => item.ProductType)
			.ToList();

		List<string> headers = ["day", "dayLabel", "product", "consumption"];
		List<IReadOnlyList<string>> rows = consumerGoods
			.SelectMany(item => dayIndexes
				.Select((day, index) => (day, index))
				.Select(pair => (IReadOnlyList<string>)new List<string>
				{
					pair.day.ToString(),
					$"D{pair.day}",
					item.Template.Name,
					item.GetLast30DaysConsumption(currentDay).ElementAtOrDefault(pair.index).ToString("0.####")
				}))
			.ToList();

		Chart sourceChart = LineChartDataSourceFactory.CreateByDimensions(
			"人口过去30天商品消费",
			headers,
			rows,
			xValueColumnIndex: 0,
			yValueColumnIndex: 3,
			xLabelColumnIndex: 1,
			dimensionColumnIndexes: [2]);

		List<DataSeries> recoloredSeries = sourceChart.SeriesList
			.Select((series, index) => DataSeries.Create(
				series.Name,
				chartColors[index % chartColors.Count].ToHtml(),
				series.Values,
				series.Key))
			.ToList();

		List<LineLegendItem> recoloredLegendItems = sourceChart.LegendItems
			.Select((legend, index) => new LineLegendItem(legend.Key, legend.Name, chartColors[index % chartColors.Count].ToHtml()))
			.ToList();

		Render(sourceChart.Update(
			legendItems: recoloredLegendItems,
			seriesList: recoloredSeries));
	}

	/// <summary>
	/// 清空图表。
	/// </summary>
	public void ClearChart()
	{
		Render(LineChartDataSourceFactory.Create("人口过去30天商品消费", Array.Empty<string>(), Array.Empty<DataSeries>()));
	}
}
