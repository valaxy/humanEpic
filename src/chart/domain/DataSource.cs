using Godot;
using Godot.Collections;

/// <summary>
/// 图表通用数据源，统一承载表格与折线图所需的数据。
/// </summary>
[GlobalClass]
public partial class DataSource : RefCounted
{
	/// <summary>
	/// 数据标题。
	/// </summary>
	public string Title { get; set; } = string.Empty;

	/// <summary>
	/// 表格头部。
	/// </summary>
	public Array<string> Headers { get; set; } = [];

	/// <summary>
	/// 表格行数据。
	/// </summary>
	public Array<Array<Variant>> Rows { get; set; } = [];

	/// <summary>
	/// 头部单元格对齐方式。
	/// </summary>
	public Array<int> HeaderAlignments { get; set; } = [];

	/// <summary>
	/// 内容单元格对齐方式。
	/// </summary>
	public Array<int> CellAlignments { get; set; } = [];

	/// <summary>
	/// 折线图 X 轴标签。
	/// </summary>
	public Array<string> XLabels { get; set; } = [];

	/// <summary>
	/// 折线图序列集合。
	/// </summary>
	public Array<DataSeries> SeriesList { get; set; } = [];

	/// <summary>
	/// 创建表格数据源。
	/// </summary>
	/// <param name="title">标题。</param>
	/// <param name="headers">头部列。</param>
	/// <param name="rows">原始行数据。</param>
	/// <param name="headerAlignments">头部对齐方式。</param>
	/// <param name="cellAlignments">内容对齐方式。</param>
	/// <returns>用于表格渲染的数据源。</returns>
	public static DataSource CreateTable(
		string title,
		Array<string> headers,
		Array rows,
		Array<int>? headerAlignments = null,
		Array<int>? cellAlignments = null)
	{
		var dataSource = new DataSource
		{
			Title = title,
			Headers = headers,
			HeaderAlignments = headerAlignments ?? [],
			CellAlignments = cellAlignments ?? []
		};

		foreach (var rowVariant in rows)
		{
			if (rowVariant.VariantType != Variant.Type.Array)
			{
				continue;
			}

			var row = rowVariant.AsGodotArray();
			var normalizedRow = new Array<Variant>();
			foreach (var cell in row)
			{
				normalizedRow.Add(cell);
			}

			dataSource.Rows.Add(normalizedRow);
		}

		return dataSource;
	}

	/// <summary>
	/// 创建折线图数据源。
	/// </summary>
	/// <param name="title">标题。</param>
	/// <param name="xLabels">X 轴标签。</param>
	/// <param name="seriesList">序列集合。</param>
	/// <returns>用于折线图渲染的数据源。</returns>
	public static DataSource CreateLineChart(string title, Array<string> xLabels, Array<DataSeries> seriesList)
	{
		return new DataSource
		{
			Title = title,
			XLabels = xLabels,
			SeriesList = seriesList
		};
	}
}
