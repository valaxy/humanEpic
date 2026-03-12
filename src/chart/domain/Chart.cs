using System;
using System.Collections.Generic;
using System.Linq;

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
    public DataSource DataSource { get; init; } = new(string.Empty, Array.Empty<string>(), Array.Empty<IReadOnlyList<string>>());

    /// <summary>
    /// 折线图 X 轴点位（数值与显示文本）。
    /// </summary>
    public IReadOnlyList<LineAxisPoint> AxisPoints { get; init; } = Array.Empty<LineAxisPoint>();

    /// <summary>
    /// 折线图图例项。
    /// </summary>
    public IReadOnlyList<LineLegendItem> LegendItems { get; init; } = Array.Empty<LineLegendItem>();

    /// <summary>
    /// 折线图序列集合。
    /// </summary>
    public IReadOnlyList<DataSeries> SeriesList { get; init; } = Array.Empty<DataSeries>();

    /// <summary>
    /// 创建图表配置。
    /// </summary>
    public static Chart Create(
        Axis xAxis,
        Axis yAxis,
        DataSource dataSource,
        IEnumerable<LineAxisPoint>? axisPoints = null,
        IEnumerable<LineLegendItem>? legendItems = null,
        IEnumerable<DataSeries>? seriesList = null)
    {
        return new Chart
        {
            XAxis = xAxis,
            YAxis = yAxis,
            DataSource = dataSource,
            AxisPoints = axisPoints == null ? Array.Empty<LineAxisPoint>() : axisPoints.ToList(),
            LegendItems = legendItems == null ? Array.Empty<LineLegendItem>() : legendItems.ToList(),
            SeriesList = seriesList == null ? Array.Empty<DataSeries>() : seriesList.ToList()
        };
    }

    /// <summary>
    /// 按需更新可视化配置或数据。
    /// </summary>
    public Chart Update(
        Axis? xAxis = null,
        Axis? yAxis = null,
        DataSource? dataSource = null,
        IEnumerable<LineAxisPoint>? axisPoints = null,
        IEnumerable<LineLegendItem>? legendItems = null,
        IEnumerable<DataSeries>? seriesList = null)
    {
        return new Chart
        {
            XAxis = xAxis ?? XAxis,
            YAxis = yAxis ?? YAxis,
            DataSource = dataSource ?? DataSource,
            AxisPoints = axisPoints == null ? AxisPoints : axisPoints.ToList(),
            LegendItems = legendItems == null ? LegendItems : legendItems.ToList(),
            SeriesList = seriesList == null ? SeriesList : seriesList.ToList()
        };
    }
}

/// <summary>
/// 折线图 X 轴点（数值 + 显示文本）。
/// </summary>
public sealed record LineAxisPoint(float Value, string Label);

/// <summary>
/// 折线图图例项。
/// </summary>
public sealed record LineLegendItem(string Key, string Name, string ColorHex);
