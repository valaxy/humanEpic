using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// CSV 列定义，声明列名、类型与解析器
/// </summary>
public abstract class CsvColumnDefinition
{
	/// <summary>
	/// 列名
	/// </summary>
	public string Header { get; }

	/// <summary>
	/// 类型标识
	/// </summary>
	public string TypeName { get; }

	/// <summary>
	/// 初始化列定义
	/// </summary>
	protected CsvColumnDefinition(string header, string typeName)
	{
		if (string.IsNullOrWhiteSpace(header))
		{
			throw new ArgumentException("CSV column header cannot be empty.", nameof(header));
		}

		Header = header.Trim();
		TypeName = typeName;
	}

	/// <summary>
	/// 解析单元格值
	/// </summary>
	public abstract object Parse(string rawValue, CsvValueContext context);

	/// <summary>
	/// 定义字符串列
	/// </summary>
	public static CsvColumnDefinition String(string header, bool allowEmpty = false)
	{
		return new CsvStringColumnDefinition(header, allowEmpty);
	}

	/// <summary>
	/// 定义布尔列（支持 1/0、true/false、yes/no）
	/// </summary>
	public static CsvColumnDefinition Boolean(string header)
	{
		return new CsvBooleanColumnDefinition(header);
	}

	/// <summary>
	/// 定义整型列
	/// </summary>
	public static CsvColumnDefinition Int(string header, int? minInclusive = null, int? maxInclusive = null)
	{
		return new CsvIntColumnDefinition(header, minInclusive, maxInclusive);
	}

	/// <summary>
	/// 定义浮点列
	/// </summary>
	public static CsvColumnDefinition Float(string header, float? minInclusive = null, float? maxInclusive = null)
	{
		return new CsvFloatColumnDefinition(header, minInclusive, maxInclusive);
	}

	/// <summary>
	/// 定义枚举列
	/// </summary>
	public static CsvColumnDefinition Enum<TEnum>(string header) where TEnum : struct, Enum
	{
		return new CsvEnumColumnDefinition<TEnum>(header);
	}

	/// <summary>
	/// 定义颜色列（支持 Html 色值与命名色）
	/// </summary>
	public static CsvColumnDefinition Color(string header)
	{
		return new CsvColorColumnDefinition(header);
	}

	/// <summary>
	/// 定义枚举到整数映射列，格式 key:value;key:value
	/// </summary>
	public static CsvColumnDefinition EnumIntDictionary<TEnum>(string header, bool allowEmpty = true, int minValue = 0) where TEnum : struct, Enum
	{
		return new CsvEnumIntDictionaryColumnDefinition<TEnum>(header, allowEmpty, minValue);
	}

	/// <summary>
	/// 定义枚举到浮点映射列，格式 key:value;key:value
	/// </summary>
	public static CsvColumnDefinition EnumFloatDictionary<TEnum>(string header, bool allowEmpty = true, float minValue = 0.0f) where TEnum : struct, Enum
	{
		return new CsvEnumFloatDictionaryColumnDefinition<TEnum>(header, allowEmpty, minValue);
	}

	protected static Dictionary<TEnum, TValue> ParseEnumDictionary<TEnum, TValue>(string rawValue, CsvValueContext context, bool allowEmpty, Func<string, TValue> valueParser)
		where TEnum : struct, Enum
		where TValue : notnull
	{
		string normalizedText = rawValue.Trim();
		if (string.IsNullOrWhiteSpace(normalizedText))
		{
			if (allowEmpty)
			{
				return new Dictionary<TEnum, TValue>();
			}

			throw context.FormatError("map value cannot be empty.");
		}

		string[] items = normalizedText.Split(';');
		Dictionary<TEnum, TValue> result = new Dictionary<TEnum, TValue>();

		items
			.Select(item => item.Trim())
			.Where(item => !string.IsNullOrWhiteSpace(item))
			.ToList()
			.ForEach(item =>
			{
				string[] pair = item.Split(':');
				if (pair.Length != 2)
				{
					throw context.FormatError($"invalid map item '{item}', expected 'key:value'.");
				}

				string keyText = pair[0].Trim();
				string valueText = pair[1].Trim();

				if (!System.Enum.TryParse(keyText, true, out TEnum key))
				{
					throw context.FormatError($"invalid enum map key '{keyText}' for {typeof(TEnum).Name}.");
				}

				if (result.ContainsKey(key))
				{
					throw context.FormatError($"duplicate map key '{keyText}'.");
				}

				TValue value = valueParser(valueText);
				result[key] = value;
			});

		return result;
	}

	protected static bool TryParseBooleanFlag(string text, out bool value)
	{
		string normalized = text.Trim().ToLowerInvariant();
		if (normalized == "1" || normalized == "true" || normalized == "yes")
		{
			value = true;
			return true;
		}

		if (normalized == "0" || normalized == "false" || normalized == "no")
		{
			value = false;
			return true;
		}

		return bool.TryParse(text, out value);
	}
}
