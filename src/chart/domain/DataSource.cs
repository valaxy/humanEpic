using System;
using System.Collections.Generic;

/// <summary>
/// 图表通用数据源，统一承载表格与折线图所需的数据。
/// </summary>
public sealed class DataSource
{
	/// <summary>
	/// 数据标题。
	/// </summary>
	public string Title { get; init; } = string.Empty;

	/// <summary>
	/// 表格头部。
	/// </summary>
	public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();

	/// <summary>
	/// 表格行数据。
	/// </summary>
	public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();

	/// <summary>
	/// 列是否为维度列（true=维度列，false=非维度列）。
	/// 未显式传入时，默认所有列都是维度列。
	/// </summary>
	public IReadOnlyList<bool> DimensionColumnFlags { get; init; } = Array.Empty<bool>();

	/// <summary>
	/// 表格数据标题。
	/// </summary>
	public string TableTitle { get; init; } = string.Empty;

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
}

/// <summary>
/// 折线图 X 轴点（数值 + 显示文本）。
/// </summary>
public sealed record LineAxisPoint(float Value, string Label);

/// <summary>
/// 折线图图例项。
/// </summary>
public sealed record LineLegendItem(string Key, string Name, string ColorHex);
