using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 图表通用数据源，统一承载表格与折线图所需的数据
/// 只管理数据本身，不负责数据的可视化和呈现细节
/// </summary>
public sealed class DataSource
{
	/// <summary>
	/// 创建空数据源。
	/// </summary>
	public DataSource()
	{
	}

	/// <summary>
	/// 通过表格结构创建数据源。
	/// </summary>
	/// <param name="title">标题。</param>
	/// <param name="headers">头部列。</param>
	/// <param name="rows">行数据，内部每行按顺序对应列。</param>
	/// <param name="dimensionColumnIndexes">维度列索引，未传入时默认全部列均为维度列。</param>
	public DataSource(
		string title,
		IEnumerable<string> headers,
		IEnumerable<IEnumerable<string>> rows,
		IEnumerable<int>? dimensionColumnIndexes = null)
	{
		List<string> headerList = headers.ToList();
		HashSet<int> dimensionIndexSet = dimensionColumnIndexes == null
			? Enumerable.Range(0, headerList.Count).ToHashSet()
			: dimensionColumnIndexes
				.Where(index => index >= 0 && index < headerList.Count)
				.ToHashSet();

		Title = title;
		Headers = headerList;
		Rows = rows.Select(row => (IReadOnlyList<string>)row.ToList()).ToList();
		DimensionColumnFlags = Enumerable.Range(0, headerList.Count)
			.Select(index => dimensionIndexSet.Contains(index))
			.ToList();
	}

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
}
