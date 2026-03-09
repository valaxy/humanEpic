using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 数据表可视化配置。
/// </summary>
public sealed class DataTable
{
	/// <summary>
	/// 表格标题。
	/// </summary>
	public string Title { get; init; } = string.Empty;

	/// <summary>
	/// 头部单元格对齐方式。
	/// </summary>
	public IReadOnlyList<DataTextAlignment> HeaderAlignments { get; init; } = Array.Empty<DataTextAlignment>();

	/// <summary>
	/// 内容单元格对齐方式。
	/// </summary>
	public IReadOnlyList<DataTextAlignment> CellAlignments { get; init; } = Array.Empty<DataTextAlignment>();

	/// <summary>
	/// 允许排序的列索引。
	/// </summary>
	public IReadOnlyList<int> SortableColumns { get; init; } = Array.Empty<int>();

	/// <summary>
	/// 创建数据表配置。
	/// </summary>
	public static DataTable Create(
		string title,
		IEnumerable<DataTextAlignment>? headerAlignments = null,
		IEnumerable<DataTextAlignment>? cellAlignments = null,
		IEnumerable<int>? sortableColumns = null)
	{
		return new DataTable
		{
			Title = title,
			HeaderAlignments = headerAlignments == null ? Array.Empty<DataTextAlignment>() : headerAlignments.ToList(),
			CellAlignments = cellAlignments == null ? Array.Empty<DataTextAlignment>() : cellAlignments.ToList(),
			SortableColumns = sortableColumns == null ? Array.Empty<int>() : sortableColumns.Distinct().ToList()
		};
	}
}
