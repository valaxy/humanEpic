using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 基于 Schema 的 CSV 严格读取器
/// </summary>
public static class CsvReader
{
	/// <summary>
	/// 读取并解析 CSV 数据行
	/// </summary>
	public static List<CsvRow> ReadRows(CsvSchema schema)
	{
		if (!FileAccess.FileExists(schema.CsvPath))
		{
			throw new InvalidOperationException($"CSV file not found: {schema.CsvPath}");
		}

		using FileAccess file = FileAccess.Open(schema.CsvPath, FileAccess.ModeFlags.Read);
		int lineNumber = 0;
		bool headerRead = false;
		List<CsvRow> rows = new List<CsvRow>();

		while (!file.EofReached())
		{
			lineNumber++;
			string line = file.GetLine();
			string trimmedLine = line.Trim();
			if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
			{
				continue;
			}

			string[] columns = line
				.Split(',')
				.Select(column => column.Trim())
				.ToArray();

			if (!headerRead)
			{
				headerRead = true;
				validateHeader(columns, schema, lineNumber);
				continue;
			}

			if (columns.Length != schema.Columns.Count)
			{
				throw new InvalidOperationException($"CSV format error in {schema.CsvPath} line {lineNumber}: expected {schema.Columns.Count} columns, got {columns.Length}.");
			}

			Dictionary<string, object> rowValues = new Dictionary<string, object>();
			Enumerable
				.Range(0, schema.Columns.Count)
				.ToList()
				.ForEach(index =>
				{
					CsvColumnDefinition columnDefinition = schema.Columns[index];
					CsvValueContext context = new CsvValueContext(schema.CsvPath, lineNumber, columnDefinition.Header);
					object parsedValue = columnDefinition.Parse(columns[index], context);
					rowValues[columnDefinition.Header] = parsedValue;
				});

			rows.Add(new CsvRow(lineNumber, rowValues));
		}

		if (!headerRead)
		{
			throw new InvalidOperationException($"CSV format error in {schema.CsvPath}: missing header row.");
		}

		return rows;
	}

	private static void validateHeader(string[] headerColumns, CsvSchema schema, int lineNumber)
	{
		if (headerColumns.Length != schema.Columns.Count)
		{
			throw new InvalidOperationException($"CSV header mismatch in {schema.CsvPath} line {lineNumber}: expected {schema.Columns.Count} columns, got {headerColumns.Length}.");
		}

		string currentHeader = string.Join(',', headerColumns);
		if (!string.Equals(currentHeader, schema.ExpectedHeader, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException($"CSV header mismatch in {schema.CsvPath}: expected '{schema.ExpectedHeader}', got '{currentHeader}'.");
		}
	}
}
