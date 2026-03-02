using System;
using Godot;

/// <summary>
/// 颜色列定义。
/// </summary>
public sealed class CsvColorColumnDefinition : CsvColumnDefinition
{
	/// <summary>
	/// 初始化颜色列定义。
	/// </summary>
	public CsvColorColumnDefinition(string header)
		: base(header, "color")
	{
	}

	/// <summary>
	/// 解析颜色值。
	/// </summary>
	public override object Parse(string rawValue, CsvValueContext context)
	{
		string text = rawValue.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw context.FormatError("color value cannot be empty.");
		}

		if (Godot.Color.HtmlIsValid(text))
		{
			return new Godot.Color(text);
		}

		Godot.Color namedColor = Godot.Color.FromString(text, new Godot.Color(0.0f, 0.0f, 0.0f, 0.0f));
		bool isNamedBlack = string.Equals(text, "black", StringComparison.OrdinalIgnoreCase);
		if (namedColor.A > 0.0f || isNamedBlack)
		{
			return namedColor;
		}

		throw context.FormatError($"invalid color value '{rawValue}'.");
	}
}
