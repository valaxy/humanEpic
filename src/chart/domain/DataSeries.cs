using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 图表序列数据，定义单条折线的名称、颜色与取值。
/// </summary>
public sealed class DataSeries
{
	/// <summary>
	/// 序列名称。
	/// </summary>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// 序列绘制颜色（HTML 颜色值，例如 #4F9DFF）。
	/// </summary>
	public string ColorHex { get; init; } = "#FFFFFF";

	/// <summary>
	/// 序列值集合。
	/// </summary>
	public IReadOnlyList<float> Values { get; init; } = Array.Empty<float>();

	/// <summary>
	/// 创建一个图表序列数据对象。
	/// </summary>
	/// <param name="name">序列名称。</param>
	/// <param name="colorHex">序列颜色（HTML 颜色值）。</param>
	/// <param name="values">序列值集合。</param>
	/// <returns>初始化后的 <see cref="DataSeries" /> 实例。</returns>
	public static DataSeries Create(string name, string colorHex, IEnumerable<float> values)
	{
		return new DataSeries
		{
			Name = name,
			ColorHex = colorHex,
			Values = values.ToList()
		};
	}
}
