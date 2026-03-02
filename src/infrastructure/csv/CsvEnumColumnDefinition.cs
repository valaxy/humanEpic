using System;

/// <summary>
/// 枚举列定义。
/// </summary>
public sealed class CsvEnumColumnDefinition<TEnum> : CsvColumnDefinition where TEnum : struct, Enum
{
	/// <summary>
	/// 初始化枚举列定义。
	/// </summary>
	public CsvEnumColumnDefinition(string header)
		: base(header, $"enum:{typeof(TEnum).Name}")
	{
	}

	/// <summary>
	/// 解析枚举值。
	/// </summary>
	public override object Parse(string rawValue, CsvValueContext context)
	{
		string text = rawValue.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw context.FormatError("enum value cannot be empty.");
		}

		if (!System.Enum.TryParse(text, true, out TEnum value))
		{
			throw context.FormatError($"invalid enum value '{rawValue}' for {typeof(TEnum).Name}.");
		}

		return value;
	}
}
