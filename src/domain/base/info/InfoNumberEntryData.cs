
/// <summary>
/// 数值型信息条目。
/// </summary>
public class InfoNumberEntryData : InfoEntryData
{
	/// <summary>
	/// 数值。
	/// </summary>
	public double Value { get; }

	/// <summary>
	/// 初始化数值型信息条目。
	/// </summary>
	/// <param name="value">数值。</param>
	public InfoNumberEntryData(double value)
	{
		Value = value;
	}

	public override string ToText()
	{
		return Value.ToString("0.##");
	}
}
