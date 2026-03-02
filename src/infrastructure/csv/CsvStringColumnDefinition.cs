using System;

/// <summary>
/// 字符串列定义。
/// </summary>
public sealed class CsvStringColumnDefinition : CsvColumnDefinition
{
	// 是否允许空字符串。
	private readonly bool allowEmpty;

	/// <summary>
	/// 初始化字符串列定义。
	/// </summary>
	public CsvStringColumnDefinition(string header, bool allowEmpty)
		: base(header, "string")
	{
		this.allowEmpty = allowEmpty;
	}

	/// <summary>
	/// 解析字符串值。
	/// </summary>
	public override object Parse(string rawValue, CsvValueContext context)
	{
		string text = rawValue.Trim();
		if (!allowEmpty && string.IsNullOrWhiteSpace(text))
		{
			throw context.FormatError("value cannot be empty.");
		}

		return text;
	}
}
