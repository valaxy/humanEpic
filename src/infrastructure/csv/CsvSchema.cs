using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CSV 解析模式，声明文件与列结构
/// </summary>
public sealed class CsvSchema
{
	/// <summary>
	/// CSV 路径
	/// </summary>
	public string CsvPath { get; }

	/// <summary>
	/// 列定义（顺序敏感）
	/// </summary>
	public IReadOnlyList<CsvColumnDefinition> Columns { get; }

	/// <summary>
	/// 预期头部字符串
	/// </summary>
	public string ExpectedHeader { get; }

	/// <summary>
	/// 初始化 CSV 解析模式
	/// </summary>
	public CsvSchema(string csvPath, IReadOnlyList<CsvColumnDefinition> columns)
	{
		if (string.IsNullOrWhiteSpace(csvPath))
		{
			throw new ArgumentException("CSV path cannot be empty.", nameof(csvPath));
		}

		if (columns.Count == 0)
		{
			throw new ArgumentException("CSV schema columns cannot be empty.", nameof(columns));
		}

		bool hasDuplicateHeaders = columns
			.GroupBy(column => column.Header, StringComparer.OrdinalIgnoreCase)
			.Any(group => group.Count() > 1);

		if (hasDuplicateHeaders)
		{
			throw new ArgumentException("CSV schema contains duplicate headers.", nameof(columns));
		}

		CsvPath = csvPath;
		Columns = columns;
		ExpectedHeader = string.Join(',', columns.Select(column => column.Header));
	}
}
