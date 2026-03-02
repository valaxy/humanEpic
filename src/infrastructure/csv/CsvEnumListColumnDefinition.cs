using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 枚举列表列定义。
/// </summary>
public sealed class CsvEnumListColumnDefinition<TEnum> : CsvColumnDefinition where TEnum : struct, Enum
{
	// 分隔符。
	private readonly char separator;

	// 是否允许空值。
	private readonly bool allowEmpty;

	// 允许值集合（为空表示不限制）。
	private readonly HashSet<TEnum>? allowedValues;

	/// <summary>
	/// 初始化枚举列表列定义。
	/// </summary>
	public CsvEnumListColumnDefinition(string header, char separator, bool allowEmpty, IEnumerable<TEnum>? allowedValues)
		: base(header, $"list<{typeof(TEnum).Name}>")
	{
		this.separator = separator;
		this.allowEmpty = allowEmpty;
		this.allowedValues = allowedValues == null ? null : new HashSet<TEnum>(allowedValues);
	}

	/// <summary>
	/// 解析枚举列表。
	/// </summary>
	public override object Parse(string rawValue, CsvValueContext context)
	{
		string normalizedText = rawValue.Trim();
		if (string.IsNullOrWhiteSpace(normalizedText))
		{
			if (allowEmpty)
			{
				return new List<TEnum>();
			}

			throw context.FormatError("enum list value cannot be empty.");
		}

		List<TEnum> parsed = normalizedText
			.Split(separator)
			.Select(item => item.Trim())
			.Where(item => !string.IsNullOrWhiteSpace(item))
			.Select(item =>
			{
				if (!System.Enum.TryParse(item, true, out TEnum enumValue))
				{
					throw context.FormatError($"invalid enum list item '{item}' for {typeof(TEnum).Name}.");
				}

				if (allowedValues != null && !allowedValues.Contains(enumValue))
				{
					string allowedText = string.Join(", ", allowedValues.Select(value => value.ToString()));
					throw context.FormatError($"enum list item '{item}' is out of allowed range, allowed values: {allowedText}.");
				}

				return enumValue;
			})
			.Distinct()
			.ToList();

		if (parsed.Count == 0 && !allowEmpty)
		{
			throw context.FormatError("enum list value cannot be empty.");
		}

		return parsed;
	}
}