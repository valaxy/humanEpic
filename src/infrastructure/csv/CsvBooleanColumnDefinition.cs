/// <summary>
/// 布尔列定义。
/// </summary>
public sealed class CsvBooleanColumnDefinition : CsvColumnDefinition
{
	/// <summary>
	/// 初始化布尔列定义。
	/// </summary>
	public CsvBooleanColumnDefinition(string header)
		: base(header, "bool")
	{
	}

	/// <summary>
	/// 解析布尔值。
	/// </summary>
	public override object Parse(string rawValue, CsvValueContext context)
	{
		if (!TryParseBooleanFlag(rawValue, out bool value))
		{
			throw context.FormatError($"invalid boolean value '{rawValue}'.");
		}

		return value;
	}
}
