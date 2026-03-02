using System.Linq;
using Godot;
using Godot.Collections;

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
        Array<string> headers = ["资源", "日产量", "质量评级", "达成率"];
        Array rows = new Array(
            Enumerable.Range(1, 8)
                .Select(index =>
                {
                    int output = 850 + index * 73;
                    string grade = index % 3 == 0 ? "A" : index % 3 == 1 ? "B" : "S";
                    string completion = $"{0.72f + index * 0.03f:P1}";
                    return new Array<Variant>
                    {
                        $"地块-{index}",
                        output,
                        grade,
                        completion
                    };
                })
                .Select(row => (Variant)row));

        Array<int> headerAlignments =
        [
            (int)HorizontalAlignment.Left,
            (int)HorizontalAlignment.Right,
            (int)HorizontalAlignment.Center,
            (int)HorizontalAlignment.Right
        ];

        Array<int> cellAlignments =
        [
            (int)HorizontalAlignment.Left,
            (int)HorizontalAlignment.Right,
            (int)HorizontalAlignment.Center,
            (int)HorizontalAlignment.Right
        ];

        return DataSource.CreateTable("资源统计表（测试数据）", headers, rows, headerAlignments, cellAlignments);
    }

    // 构建折线图演示数据。
    private static DataSource createLineChartDemoData()
    {
        Array<string> xLabels = new Array<string>(Enumerable.Range(1, 12).Select(month => $"{month}月"));

        Array<float> wheatSeriesValues = new Array<float>(
            Enumerable.Range(0, 12).Select(index => 60f + Mathf.Sin(index * 0.55f) * 18f + index * 1.1f));

        Array<float> cornSeriesValues = new Array<float>(
            Enumerable.Range(0, 12).Select(index => 45f + Mathf.Cos(index * 0.48f) * 14f + index * 1.3f));

        DataSeries wheatSeries = DataSeries.Create("小麦", Colors.CornflowerBlue, wheatSeriesValues);
        DataSeries cornSeries = DataSeries.Create("玉米", Colors.Orange, cornSeriesValues);
        Array<DataSeries> seriesList = [wheatSeries, cornSeries];

        return DataSource.CreateLineChart("年度产量趋势（测试数据）", xLabels, seriesList);
    }
}
