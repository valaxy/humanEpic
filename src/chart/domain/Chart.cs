using System;

/// <summary>
/// 图表配置入口，组合可视化配置与数据。
/// </summary>
public sealed class Chart
{
    /// <summary>
    /// X 轴配置。
    /// </summary>
    public Axis XAxis { get; init; } = Axis.Create("X");

    /// <summary>
    /// Y 轴配置。
    /// </summary>
    public Axis YAxis { get; init; } = Axis.Create("Y");

    /// <summary>
    /// 折线图数据。
    /// </summary>
    public DataSource DataSource { get; init; } = DataSource.CreateLineChart(string.Empty, Array.Empty<string>(), Array.Empty<DataSeries>());

    /// <summary>
    /// 创建图表配置。
    /// </summary>
    public static Chart Create(Axis xAxis, Axis yAxis, DataSource dataSource)
    {
        return new Chart
        {
            XAxis = xAxis,
            YAxis = yAxis,
            DataSource = dataSource
        };
    }

    /// <summary>
    /// 按需更新可视化配置或数据。
    /// </summary>
    public Chart Update(Axis? xAxis = null, Axis? yAxis = null, DataSource? dataSource = null)
    {
        return new Chart
        {
            XAxis = xAxis ?? XAxis,
            YAxis = yAxis ?? YAxis,
            DataSource = dataSource ?? DataSource
        };
    }
}
