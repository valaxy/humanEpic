using Godot;

/// <summary>
/// 文本型信息条目。
/// </summary>
[GlobalClass]
public partial class InfoTextEntryData : InfoEntryData
{
    /// <summary>
    /// 文本值。
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// 初始化文本型信息条目。
    /// </summary>
    /// <param name="value">文本值。</param>
    public InfoTextEntryData(string value)
    {
        Value = value;
    }

    public override string ToText()
    {
        return Value;
    }
}
