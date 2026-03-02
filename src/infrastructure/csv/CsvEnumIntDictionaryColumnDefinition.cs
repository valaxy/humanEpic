using System;

/// <summary>
/// 枚举到整数字典列定义。
/// </summary>
public sealed class CsvEnumIntDictionaryColumnDefinition<TEnum> : CsvColumnDefinition where TEnum : struct, Enum
{
	// 是否允许空值。
	private readonly bool allowEmpty;

	// 最小值（包含）。
	private readonly int minValue;

	/// <summary>
	/// 初始化枚举到整数字典列定义。
	/// </summary>
	public CsvEnumIntDictionaryColumnDefinition(string header, bool allowEmpty, int minValue)
		: base(header, $"map<{typeof(TEnum).Name},int>")
	{
		this.allowEmpty = allowEmpty;
		this.minValue = minValue;
	}

	/// <summary>
	/// 解析枚举到整数字典。
	/// </summary>
	public override object Parse(string rawValue, CsvValueContext context)
	{
		return ParseEnumDictionary<TEnum, int>(
			rawValue,
			context,
			allowEmpty,
			valueText =>
			{
				if (!int.TryParse(valueText, out int parsedValue))
				{
					throw context.FormatError($"invalid int map value '{valueText}'.");
				}

				if (parsedValue < minValue)
				{
					throw context.FormatError($"int map value must be >= {minValue}, got {parsedValue}.");
				}

				return parsedValue;
			});
	}
}
