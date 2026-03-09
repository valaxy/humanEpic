using System;
using System.Collections.Generic;
using System.Linq;

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
	/// 表格数据标题。
	/// </summary>
	public string TableTitle { get; init; } = string.Empty;

	/// <summary>
	/// 折线图 X 轴标签。
	/// </summary>
	public IReadOnlyList<string> XLabels { get; init; } = Array.Empty<string>();

	/// <summary>
	/// 折线图 X 轴数值。
	/// </summary>
	public IReadOnlyList<float> XValues { get; init; } = Array.Empty<float>();

	/// <summary>
	/// 折线图序列集合。
	/// </summary>
	public IReadOnlyList<DataSeries> SeriesList { get; init; } = Array.Empty<DataSeries>();

	/// <summary>
	/// 创建表格数据源。
	/// </summary>
	/// <param name="title">标题。</param>
	/// <param name="headers">头部列。</param>
	/// <param name="rows">行数据，内部每行按顺序对应列。</param>
	/// <returns>用于表格渲染的数据源。</returns>
	public static DataSource CreateTable(
		string title,
		IEnumerable<string> headers,
		IEnumerable<IEnumerable<string>> rows)
	{
		return new DataSource
		{
			TableTitle = title,
			Headers = headers.ToList(),
			Rows = rows.Select(row => (IReadOnlyList<string>)row.ToList()).ToList()
		};
	}

	/// <summary>
	/// 创建折线图数据源。
	/// </summary>
	/// <param name="title">标题。</param>
	/// <param name="xLabels">X 轴标签。</param>
	/// <param name="seriesList">序列集合。</param>
	/// <param name="xValues">X 轴数值，可选；未提供时按索引（0..n-1）处理。</param>
	/// <returns>用于折线图渲染的数据源。</returns>
	public static DataSource CreateLineChart(
		string title,
		IEnumerable<string> xLabels,
		IEnumerable<DataSeries> seriesList,
		IEnumerable<float>? xValues = null)
	{
		return new DataSource
		{
			Title = title,
			XLabels = xLabels.ToList(),
			SeriesList = seriesList.ToList(),
			XValues = xValues == null ? Array.Empty<float>() : xValues.ToList()
		};
	}
}
