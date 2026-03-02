using System;
using System.Globalization;

/// <summary>
/// 枚举到浮点数字典列定义。
/// </summary>
public sealed class CsvEnumFloatDictionaryColumnDefinition<TEnum> : CsvColumnDefinition where TEnum : struct, Enum
{
	// 是否允许空值。
	private readonly bool allowEmpty;

	// 最小值（包含）。
	private readonly float minValue;

	/// <summary>
	/// 初始化枚举到浮点数字典列定义。
	/// </summary>
	public CsvEnumFloatDictionaryColumnDefinition(string header, bool allowEmpty, float minValue)
		: base(header, $"map<{typeof(TEnum).Name},float>")
	{
		this.allowEmpty = allowEmpty;
		this.minValue = minValue;
	}

	/// <summary>
	/// 解析枚举到浮点数字典。
	/// </summary>
	public override object Parse(string rawValue, CsvValueContext context)
	{
		return ParseEnumDictionary<TEnum, float>(
			rawValue,
			context,
			allowEmpty,
			valueText =>
			{
				if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
				{
					throw context.FormatError($"invalid float map value '{valueText}'.");
				}

				if (parsedValue < minValue)
				{
					throw context.FormatError($"float map value must be >= {minValue.ToString(CultureInfo.InvariantCulture)}, got {parsedValue.ToString(CultureInfo.InvariantCulture)}.");
				}

				return parsedValue;
			});
	}
}
