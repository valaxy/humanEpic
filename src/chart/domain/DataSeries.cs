using Godot;
using Godot.Collections;

/// <summary>
/// 图表序列数据，定义单条折线的名称、颜色与取值。
/// </summary>
[GlobalClass]
public partial class DataSeries : RefCounted
{
	/// <summary>
	/// 序列名称。
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 序列绘制颜色。
	/// </summary>
	public Color Color { get; set; } = Colors.White;

	/// <summary>
	/// 序列值集合。
	/// </summary>
	public Array<float> Values { get; set; } = [];

	/// <summary>
	/// 创建一个图表序列数据对象。
	/// </summary>
	/// <param name="name">序列名称。</param>
	/// <param name="color">序列颜色。</param>
	/// <param name="values">序列值集合。</param>
	/// <returns>初始化后的 <see cref="DataSeries" /> 实例。</returns>
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
