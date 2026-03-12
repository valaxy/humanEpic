using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 折线图独立 Demo 入口。
/// </summary>
[GlobalClass]
public partial class LineChartViewDemo : Control
{
    // 折线图组件。
    private LineChartView lineChart = null!;

    // 当前图表配置。
    private Chart chart = null!;

    /// <summary>
    /// 初始化并渲染 Demo。
    /// </summary>
    public override void _Ready()
    {
        lineChart = GetNode<LineChartView>("%LineChart");
        renderInitial();
        CallDeferred(MethodName.refreshWithNewData);
    }

    // 首次渲染。
    private void renderInitial()
    {
        chart = createChart(0.0f).Update(
            xAxis: Axis.Create("月份", 2.0f, 10.0f, TickFormatter.Custom(value => $"M{value:0}")),
            yAxis: Axis.Create("产量", 40.0f, 92.0f, TickFormatter.Number(1), Tick.PowerOfTen()));
        lineChart.UpdateChart(chart);
    }

    // 演示通过 Update 仅刷新数据。
    private void refreshWithNewData()
    {
        Chart nextChart = createChart(1.0f);
        Chart updatedChart = chart.Update(
            dataSource: nextChart.DataSource,
            axisPoints: nextChart.AxisPoints,
            legendItems: nextChart.LegendItems,
            seriesList: nextChart.SeriesList);
        lineChart.UpdateChart(updatedChart);
        chart = updatedChart;
    }

    // 构建折线图数据。
    private static Chart createChart(float phaseShift)
    {
        List<float> xValues = Enumerable.Range(1, 12).Select(index => (float)index).ToList();
        List<string> xLabels = xValues.Select(value => $"{value:0}月").ToList();

        List<float> wheatValues = xValues
            .Select(x => 58.0f + Mathf.Sin((x + phaseShift) * 0.45f) * 12.0f + x * 1.4f)
            .ToList();

        List<float> riceValues = xValues
            .Select(x => 52.0f + Mathf.Cos((x + phaseShift) * 0.4f) * 10.0f + x * 1.2f)
            .ToList();

        List<DataSeries> seriesList =
        [
            DataSeries.Create("小麦", Colors.CornflowerBlue.ToHtml(), wheatValues),
            DataSeries.Create("水稻", Colors.OrangeRed.ToHtml(), riceValues)
        ];

        return LineChartDataSourceFactory.Create("年度产量趋势", xLabels, seriesList, xValues);
    }
}
