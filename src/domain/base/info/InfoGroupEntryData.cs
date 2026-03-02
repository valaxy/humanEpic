using System;
using Godot;

/// <summary>
/// 分组型信息条目。
/// </summary>
[GlobalClass]
public partial class InfoGroupEntryData : InfoEntryData
{
    /// <summary>
    /// 分组数据。
    /// </summary>
    public InfoData Value { get; }

    /// <summary>
    /// 初始化分组型信息条目。
    /// </summary>
    /// <param name="value">分组数据。</param>
    public InfoGroupEntryData(InfoData value)
    {
        Value = value;
    }

    public override string ToText()
    {
        return String.Empty; // 分组本身不直接展示文本
    }
}
