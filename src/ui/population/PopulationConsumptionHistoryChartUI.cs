using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 人口过去 30 天商品消费折线图组件。
/// </summary>
[GlobalClass]
public partial class PopulationConsumptionHistoryChartUI : LineChart
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
		List<string> xLabels = dayIndexes
			.Select(day => $"D{day}")
			.ToList();

		List<AssetItem> consumerGoods = population.Asset.GetAll()
			.Where(item => item.Template.IsConsumerGood)
			.OrderBy(item => item.ProductType)
			.ToList();

		List<DataSeries> seriesList = consumerGoods
			.Select((item, index) => DataSeries.Create(
				item.Template.Name,
				chartColors[index % chartColors.Count].ToHtml(),
				item.GetLast30DaysConsumption(currentDay)))
			.ToList();

		Render(DataSource.CreateLineChart("人口过去30天商品消费", xLabels, seriesList, dayIndexes.Select(day => (float)day)));
	}

	/// <summary>
	/// 清空图表。
	/// </summary>
	public void ClearChart()
	{
		Render(DataSource.CreateLineChart("人口过去30天商品消费", Array.Empty<string>(), Array.Empty<DataSeries>()));
	}
}
