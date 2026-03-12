using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 劳动力市场价格历史折线图组件。
/// </summary>
[GlobalClass]
public partial class LabourMarketHistoryChartUI : LineChartView
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
	/// 根据劳动力市场历史数据刷新折线图。
	/// </summary>
	public void RenderMarket(LabourMarket market)
	{
		List<PriceHistorySnapshot<JobType.Enums>> snapshots = market.PriceHistory.Snapshots.ToList();
		List<string> xLabels = snapshots.Select(snapshot => snapshot.Dt).ToList();
		List<DataSeries> seriesList = Enum.GetValues<JobType.Enums>()
			.Select((jobType, index) => DataSeries.Create(
				JobTemplate.GetTemplate(jobType).Name,
				chartColors[index % chartColors.Count].ToHtml(),
				snapshots.Select(snapshot => getSnapshotPrice(snapshot, jobType)).ToList()))
			.ToList();

		DataSource source = DataSource.CreateLineChart("劳动力价格历史（dt）", xLabels, seriesList);
		Render(source);
	}

	// 获取快照中某职业的价格。
	private static float getSnapshotPrice(PriceHistorySnapshot<JobType.Enums> snapshot, JobType.Enums jobType)
	{
		return snapshot.Prices.TryGetValue(jobType, out float price)
			? price
			: 0.0f;
	}
}
