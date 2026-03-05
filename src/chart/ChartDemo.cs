using System.Linq;
using Godot;
using System.Collections.Generic;

/// <summary>
/// Chart 组件测试入口，集中展示常用图表组件。
/// </summary>
[GlobalClass]
public partial class ChartDemo : Control
{
    // 表格演示组件。
    private ReusableDataTable tableDemo = null!;

    // 折线图演示组件。
    private LineChart lineChartDemo = null!;

    /// <summary>
    /// 初始化并渲染所有 Demo 数据。
    /// </summary>
    public override void _Ready()
    {
        tableDemo = GetNode<ReusableDataTable>("%TableDemo");
        lineChartDemo = GetNode<LineChart>("%LineChartDemo");
        renderDemos();
    }

    // 渲染全部组件演示。
    private void renderDemos()
    {
        DataSource tableDataSource = createTableDemoData();
        DataSource lineChartDataSource = createLineChartDemoData();
        tableDemo.Render(tableDataSource);
        lineChartDemo.Render(lineChartDataSource);
    }

    // 构建表格演示数据。
    private static DataSource createTableDemoData()
    {
        List<string> headers = ["资源", "日产量", "质量评级", "达成率"];
        List<List<string>> rows = Enumerable.Range(1, 8)
            .Select(index =>
            {
                int output = 850 + index * 73;
                string grade = index % 3 == 0 ? "A" : index % 3 == 1 ? "B" : "S";
                string completion = $"{0.72f + index * 0.03f:P1}";
                return new List<string>
                {
                    $"地块-{index}",
                    output.ToString(),
                    grade,
                    completion
                };
            })
            .ToList();

        List<DataTextAlignment> headerAlignments =
        [
            DataTextAlignment.Left,
            DataTextAlignment.Right,
            DataTextAlignment.Center,
            DataTextAlignment.Right
        ];

        List<DataTextAlignment> cellAlignments =
        [
            DataTextAlignment.Left,
            DataTextAlignment.Right,
            DataTextAlignment.Center,
            DataTextAlignment.Right
        ];

        return DataSource.CreateTable("资源统计表（测试数据）", headers, rows, headerAlignments, cellAlignments);
    }

    // 构建折线图演示数据。
    private static DataSource createLineChartDemoData()
    {
        List<string> xLabels = Enumerable.Range(1, 12).Select(month => $"{month}月").ToList();

        List<float> wheatSeriesValues = Enumerable.Range(0, 12)
            .Select(index => 60f + Mathf.Sin(index * 0.55f) * 18f + index * 1.1f)
            .ToList();

        List<float> cornSeriesValues = Enumerable.Range(0, 12)
            .Select(index => 45f + Mathf.Cos(index * 0.48f) * 14f + index * 1.3f)
            .ToList();

        DataSeries wheatSeries = DataSeries.Create("小麦", Colors.CornflowerBlue.ToHtml(), wheatSeriesValues);
        DataSeries cornSeries = DataSeries.Create("玉米", Colors.Orange.ToHtml(), cornSeriesValues);
        List<DataSeries> seriesList = [wheatSeries, cornSeries];

        return DataSource.CreateLineChart("年度产量趋势（测试数据）", xLabels, seriesList);
    }
}
