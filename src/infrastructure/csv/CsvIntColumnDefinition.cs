/// <summary>
/// 整型列定义。
/// </summary>
public sealed class CsvIntColumnDefinition : CsvColumnDefinition
{
	// 最小值（包含）。
	private readonly int? minInclusive;

	// 最大值（包含）。
	private readonly int? maxInclusive;

	/// <summary>
	/// 初始化整型列定义。
	/// </summary>
	public CsvIntColumnDefinition(string header, int? minInclusive, int? maxInclusive)
		: base(header, "int")
	{
		this.minInclusive = minInclusive;
		this.maxInclusive = maxInclusive;
	}

	/// <summary>
	/// 解析整型值。
	/// </summary>
	public override object Parse(string rawValue, CsvValueContext context)
	{
		if (!int.TryParse(rawValue.Trim(), out int value))
		{
			throw context.FormatError($"invalid int value '{rawValue}'.");
		}

		if (minInclusive.HasValue && value < minInclusive.Value)
		{
			throw context.FormatError($"int value must be >= {minInclusive.Value}, got {value}.");
		}

		if (maxInclusive.HasValue && value > maxInclusive.Value)
		{
			throw context.FormatError($"int value must be <= {maxInclusive.Value}, got {value}.");
		}

		return value;
	}
}
