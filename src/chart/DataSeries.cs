using Godot;
using Godot.Collections;

/// <summary>
/// 图表序列数据，定义单条折线的名称、颜色与取值。
/// </summary>
[GlobalClass]
public partial class DataSeries : RefCounted
{
    public string Name { get; set; } = string.Empty;

    public Color Color { get; set; } = Colors.White;

    public Array<float> Values { get; set; } = [];

    public static DataSeries Create(string name, Color color, Array<float> values)
    {
        return new DataSeries
        {
            Name = name,
            Color = color,
            Values = values
        };
    }
}