/// <summary>
/// CSV 值辅助工具
/// </summary>
public static class CsvValueUtility
{
	/// <summary>
	/// 归一化文本键（去空格并转大写）
	/// </summary>
	public static string NormalizeKey(string key)
	{
		return key.Trim().ToUpperInvariant();
	}
}
