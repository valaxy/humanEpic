using Godot;

/// <summary>
/// 布尔型信息条目。
/// </summary>
[GlobalClass]
public partial class InfoBooleanEntryData : InfoEntryData
{
    /// <summary>
    /// 布尔值。
    /// </summary>
    public bool Value { get; }

    /// <summary>
    /// 初始化布尔型信息条目。
    /// </summary>
    /// <param name="value">布尔值。</param>
    public InfoBooleanEntryData(bool value)
    {
        Value = value;
    }

    public override string ToText()
    {
        return Value ? "是" : "否";
    }
}
