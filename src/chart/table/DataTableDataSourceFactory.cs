using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 表格数据源构建器，负责将原始二维数据转换为 DataSource。
/// </summary>
public static class DataTableDataSourceFactory
{
	/// <summary>
	/// 创建表格数据源。
	/// </summary>
	/// <param name="title">标题。</param>
	/// <param name="headers">头部列。</param>
	/// <param name="rows">行数据，内部每行按顺序对应列。</param>
	/// <param name="dimensionColumnIndexes">维度列索引，未传入时默认全部列均为维度列。</param>
	/// <returns>用于表格渲染的数据源。</returns>
	public static DataSource Create(
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

		return new DataSource
		{
			TableTitle = title,
			Headers = headerList,
			Rows = rows.Select(row => (IReadOnlyList<string>)row.ToList()).ToList(),
			DimensionColumnFlags = Enumerable.Range(0, headerList.Count)
				.Select(index => dimensionIndexSet.Contains(index))
				.ToList()
		};
	}
}
