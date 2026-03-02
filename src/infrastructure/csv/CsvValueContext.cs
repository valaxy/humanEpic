using System;

/// <summary>
/// CSV 值解析上下文
/// </summary>
public readonly struct CsvValueContext
{
	/// <summary>
	/// CSV 文件路径
	/// </summary>
	public string CsvPath { get; }

	/// <summary>
	/// 当前行号
	/// </summary>
	public int LineNumber { get; }

	/// <summary>
	/// 当前列名
	/// </summary>
	public string Header { get; }

	/// <summary>
	/// 初始化 CSV 值解析上下文
	/// </summary>
	public CsvValueContext(string csvPath, int lineNumber, string header)
	{
		CsvPath = csvPath;
		LineNumber = lineNumber;
		Header = header;
	}

	/// <summary>
	/// 构建格式错误异常
	/// </summary>
	public InvalidOperationException FormatError(string message)
	{
		return new InvalidOperationException($"CSV format error in {CsvPath} line {LineNumber}, column '{Header}': {message}");
	}
}
