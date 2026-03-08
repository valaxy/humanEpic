/// <summary>
/// 折线图坐标轴配置。
/// </summary>
public sealed class Axis
{
    /// <summary>
    /// 轴名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 刻度最小值（可选）。
    /// </summary>
    public float? MinTick { get; init; }

    /// <summary>
    /// 刻度最大值（可选）。
    /// </summary>
    public float? MaxTick { get; init; }

    /// <summary>
    /// 刻度格式化器。
    /// </summary>
    public TickFormatter TickFormatter { get; init; } = TickFormatter.Default;

    /// <summary>
    /// 判断数值是否落在轴范围内。
    /// </summary>
    public bool IsInRange(float value)
    {
        bool hitMin = !MinTick.HasValue || value >= MinTick.Value;
        bool hitMax = !MaxTick.HasValue || value <= MaxTick.Value;
        return hitMin && hitMax;
    }

    /// <summary>
    /// 格式化轴刻度文本。
    /// </summary>
    public string Format(float value)
    {
        return TickFormatter.Format(value);
    }

    /// <summary>
    /// 创建轴配置。
    /// </summary>
    public static Axis Create(string name, float? minTick = null, float? maxTick = null, TickFormatter? tickFormatter = null)
    {
        return new Axis
        {
            Name = name,
            MinTick = minTick,
            MaxTick = maxTick,
            TickFormatter = tickFormatter ?? TickFormatter.Default
        };
    }
}
