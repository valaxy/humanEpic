using Godot;
using Godot.Collections;

/// <summary>
/// 图表通用数据源，统一承载表格与折线图所需的数据。
/// </summary>
[GlobalClass]
public partial class DataSource : RefCounted
{
	public string Title { get; set; } = string.Empty;

	public Array<string> Headers { get; set; } = [];

	public Array<Array<Variant>> Rows { get; set; } = [];

	public Array<int> HeaderAlignments { get; set; } = [];

	public Array<int> CellAlignments { get; set; } = [];

	public Array<string> XLabels { get; set; } = [];

	public Array<DataSeries> SeriesList { get; set; } = [];

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
