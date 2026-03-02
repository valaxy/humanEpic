using System.Globalization;

/// <summary>
/// 浮点列定义。
/// </summary>
public sealed class CsvFloatColumnDefinition : CsvColumnDefinition
{
	// 最小值（包含）。
	private readonly float? minInclusive;

	// 最大值（包含）。
	private readonly float? maxInclusive;

	/// <summary>
	/// 初始化浮点列定义。
	/// </summary>
	public CsvFloatColumnDefinition(string header, float? minInclusive, float? maxInclusive)
		: base(header, "float")
	{
		this.minInclusive = minInclusive;
		this.maxInclusive = maxInclusive;
	}

	/// <summary>
	/// 解析浮点值。
	/// </summary>
	public override object Parse(string rawValue, CsvValueContext context)
	{
		if (!float.TryParse(rawValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
		{
			throw context.FormatError($"invalid float value '{rawValue}'.");
		}

		if (minInclusive.HasValue && value < minInclusive.Value)
		{
			throw context.FormatError($"float value must be >= {minInclusive.Value.ToString(CultureInfo.InvariantCulture)}, got {value.ToString(CultureInfo.InvariantCulture)}.");
		}

		if (maxInclusive.HasValue && value > maxInclusive.Value)
		{
			throw context.FormatError($"float value must be <= {maxInclusive.Value.ToString(CultureInfo.InvariantCulture)}, got {value.ToString(CultureInfo.InvariantCulture)}.");
		}

		return value;
	}
}
