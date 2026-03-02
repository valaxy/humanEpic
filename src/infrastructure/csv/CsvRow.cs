using System;
using System.Collections.Generic;

/// <summary>
/// 单行 CSV 解析结果
/// </summary>
public sealed class CsvRow
{
	private readonly Dictionary<string, object> values;

	/// <summary>
	/// 数据行号
	/// </summary>
	public int LineNumber { get; }

	/// <summary>
	/// 初始化 CSV 行
	/// </summary>
	public CsvRow(int lineNumber, Dictionary<string, object> rowValues)
	{
		LineNumber = lineNumber;
		values = rowValues;
	}

	/// <summary>
	/// 获取指定列的强类型值
	/// </summary>
	public T Get<T>(string header)
	{
		if (!values.TryGetValue(header, out object? value))
		{
			throw new InvalidOperationException($"CSV parsed row does not contain header '{header}' at line {LineNumber}.");
		}

		if (value is not T typedValue)
		{
			throw new InvalidOperationException($"CSV parsed value type mismatch at line {LineNumber}, header '{header}', expected {typeof(T).Name}.");
		}

		return typedValue;
	}
}
