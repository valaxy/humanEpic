using Godot;

/// <summary>
/// 信息条目数据的抽象基类。
/// </summary>
public abstract class InfoEntryData
{
	/// <summary>
	/// 将条目解析为文本。
	/// </summary>
	public abstract string ToText();
}