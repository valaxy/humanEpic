using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// 折线图数据源构建器，负责组织轴点、图例与序列映射。
/// </summary>
public static class LineChartDataSourceFactory
{
	// 默认调色板。
	private static readonly IReadOnlyList<string> defaultColorPalette =
	[
		"#5B8FF9",
		"#F6BD16",
		"#5AD8A6",
		"#E8684A",
		"#6DC8EC",
		"#9270CA",
		"#FF9D4D",
		"#269A99"
	];

	/// <summary>
	/// 创建折线图数据源。
	/// </summary>
	/// <param name="title">标题。</param>
	/// <param name="xLabels">X 轴标签。</param>
	/// <param name="seriesList">序列集合。</param>
	/// <param name="xValues">X 轴数值，可选；未提供时按索引（0..n-1）处理。</param>
	public static DataSource Create(
		string title,
		IEnumerable<string> xLabels,
		IEnumerable<DataSeries> seriesList,
		IEnumerable<float>? xValues = null)
	{
		List<DataSeries> normalizedSeriesList = seriesList
			.Select(series => DataSeries.Create(series.Name, series.ColorHex, series.Values, normalizeSeriesKey(series)))
			.ToList();

		List<string> labelList = xLabels.ToList();
		List<float> valueList = xValues == null ? [] : xValues.ToList();
		int axisCount = new[]
		{
			labelList.Count,
			valueList.Count,
			normalizedSeriesList.Select(series => series.Values.Count).DefaultIfEmpty(0).Max()
		}.Max();

		List<LineAxisPoint> axisPoints = Enumerable.Range(0, axisCount)
			.Select(index => new LineAxisPoint(
				index < valueList.Count ? valueList[index] : index,
				index < labelList.Count
					? labelList[index]
					: formatDefaultAxisLabel(index < valueList.Count ? valueList[index] : index)))
			.ToList();

		List<LineLegendItem> legendItems = normalizedSeriesList
			.Select(series => new LineLegendItem(normalizeSeriesKey(series), series.Name, series.ColorHex))
			.ToList();

		return new DataSource
		{
			Title = title,
			SeriesList = normalizedSeriesList,
			AxisPoints = axisPoints,
			LegendItems = legendItems
		};
	}

	/// <summary>
	/// 从多维表格数据创建折线图数据源。
	/// 维度列将用于拆分图例与折线，非维度列作为数值指标。
	/// </summary>
	public static DataSource CreateByDimensions(
		string title,
		IEnumerable<string> headers,
		IEnumerable<IEnumerable<string>> rows,
		int xValueColumnIndex,
		int yValueColumnIndex,
		int? xLabelColumnIndex = null,
		IEnumerable<int>? dimensionColumnIndexes = null)
	{
		DataSource tableSource = DataTableDataSourceFactory.Create(title, headers, rows, dimensionColumnIndexes);
		List<string> headerList = tableSource.Headers.ToList();

		if (headerList.Count == 0)
		{
			return Create(title, Array.Empty<string>(), Array.Empty<DataSeries>());
		}

		int safeXValueColumnIndex = clampColumnIndex(xValueColumnIndex, headerList.Count);
		int safeYValueColumnIndex = clampColumnIndex(yValueColumnIndex, headerList.Count);
		int effectiveXLabelColumnIndex = xLabelColumnIndex.HasValue
			? clampColumnIndex(xLabelColumnIndex.Value, headerList.Count)
			: safeXValueColumnIndex;

		List<int> splitDimensionIndexes = Enumerable.Range(0, headerList.Count)
			.Where(index => tableSource.DimensionColumnFlags.ElementAtOrDefault(index))
			.Where(index => index != safeXValueColumnIndex && index != safeYValueColumnIndex)
			.ToList();

		List<(float XValue, string XLabel)> xAxisCandidates = tableSource.Rows
			.Select((row, rowIndex) =>
			{
				float xValue = parseFloatOrFallback(getCellValue(row, safeXValueColumnIndex), rowIndex);
				string xLabel = getCellValue(row, effectiveXLabelColumnIndex);
				return (XValue: xValue, XLabel: string.IsNullOrWhiteSpace(xLabel) ? formatDefaultAxisLabel(xValue) : xLabel);
			})
			.GroupBy(item => item.XValue)
			.Select(group => group.First())
			.OrderBy(item => item.XValue)
			.ToList();

		List<float> xAxisValues = xAxisCandidates.Select(item => item.XValue).ToList();
		List<string> xAxisLabels = xAxisCandidates.Select(item => item.XLabel).ToList();

		List<(string Key, string Name, string ColorHex, IReadOnlyList<float> Values)> groupedSeries = tableSource.Rows
			.Select((row, rowIndex) =>
			{
				float xValue = parseFloatOrFallback(getCellValue(row, safeXValueColumnIndex), rowIndex);
				float yValue = parseFloatOrFallback(getCellValue(row, safeYValueColumnIndex), 0.0f);
				string key = buildDimensionKey(row, splitDimensionIndexes);
				string name = buildDimensionName(row, splitDimensionIndexes, headerList);
				return (xValue, yValue, key, name);
			})
			.GroupBy(item => item.key)
			.Select((group, colorIndex) =>
			{
				Dictionary<float, float> pointMap = group
					.GroupBy(item => item.xValue)
					.ToDictionary(pointGroup => pointGroup.Key, pointGroup => pointGroup.Last().yValue);

				IReadOnlyList<float> values = xAxisValues
					.Select(x => pointMap.TryGetValue(x, out float y) ? y : float.NaN)
					.ToList();

				return (
					Key: group.Key,
					Name: group.Select(item => item.name).First(),
					ColorHex: defaultColorPalette[colorIndex % defaultColorPalette.Count],
					Values: values);
			})
			.ToList();

		List<DataSeries> seriesList = groupedSeries
			.Select(item => DataSeries.Create(item.Name, item.ColorHex, item.Values, item.Key))
			.ToList();

		List<LineLegendItem> legendItems = groupedSeries
			.Select(item => new LineLegendItem(item.Key, item.Name, item.ColorHex))
			.ToList();

		DataSource lineSource = Create(title, xAxisLabels, seriesList, xAxisValues);
		return new DataSource
		{
			Title = lineSource.Title,
			Headers = tableSource.Headers,
			Rows = tableSource.Rows,
			TableTitle = tableSource.TableTitle,
			DimensionColumnFlags = tableSource.DimensionColumnFlags,
			AxisPoints = lineSource.AxisPoints,
			LegendItems = legendItems,
			SeriesList = lineSource.SeriesList
		};
	}

	/// <summary>
	/// 返回序列稳定键，优先使用显式 Key。
	/// </summary>
	public static string ResolveSeriesKey(DataSeries series)
	{
		return normalizeSeriesKey(series);
	}

	private static int clampColumnIndex(int index, int count)
	{
		return Math.Clamp(index, 0, Math.Max(0, count - 1));
	}

	private static string getCellValue(IReadOnlyList<string> row, int columnIndex)
	{
		return columnIndex >= 0 && columnIndex < row.Count
			? row[columnIndex]
			: string.Empty;
	}

	private static float parseFloatOrFallback(string raw, float fallback)
	{
		if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float invariantValue))
		{
			return invariantValue;
		}

		if (float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out float currentCultureValue))
		{
			return currentCultureValue;
		}

		return fallback;
	}

	private static string buildDimensionKey(IReadOnlyList<string> row, IReadOnlyList<int> splitDimensionIndexes)
	{
		if (splitDimensionIndexes.Count == 0)
		{
			return "__all";
		}

		return string.Join("|", splitDimensionIndexes.Select(index => getCellValue(row, index)));
	}

	private static string buildDimensionName(IReadOnlyList<string> row, IReadOnlyList<int> splitDimensionIndexes, IReadOnlyList<string> headers)
	{
		if (splitDimensionIndexes.Count == 0)
		{
			return "总量";
		}

		if (splitDimensionIndexes.Count == 1)
		{
			return getCellValue(row, splitDimensionIndexes[0]);
		}

		return string.Join(" | ", splitDimensionIndexes.Select(index =>
		{
			string header = headers.ElementAtOrDefault(index) ?? $"Col{index}";
			return $"{header}:{getCellValue(row, index)}";
		}));
	}

	private static string formatDefaultAxisLabel(float value)
	{
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}

	private static string normalizeSeriesKey(DataSeries series)
	{
		if (!string.IsNullOrWhiteSpace(series.Key))
		{
			return series.Key;
		}

		if (!string.IsNullOrWhiteSpace(series.Name))
		{
			return series.Name;
		}

		return Guid.NewGuid().ToString("N");
	}
}
