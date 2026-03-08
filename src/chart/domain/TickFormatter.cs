using System;
using System.Globalization;

/// <summary>
/// 坐标轴刻度文本格式化器。
/// </summary>
public sealed class TickFormatter
{
    // 默认格式化器（原样数字）。
    public static TickFormatter Default { get; } = new TickFormatter(value => value.ToString("0.##", CultureInfo.InvariantCulture));

    // 刻度格式化函数。
    private readonly Func<float, string> formatter;

    private TickFormatter(Func<float, string> formatter)
    {
        this.formatter = formatter;
    }

    /// <summary>
    /// 应用格式化。
    /// </summary>
    public string Format(float value)
    {
        return formatter(value);
    }

    /// <summary>
    /// 使用小数位格式。
    /// </summary>
    public static TickFormatter Number(int decimals)
    {
        string format = decimals <= 0 ? "0" : $"0.{new string('0', decimals)}";
        return new TickFormatter(value => value.ToString(format, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 使用百分比格式。
    /// </summary>
    public static TickFormatter Percent(int decimals)
    {
        string format = decimals <= 0 ? "P0" : $"P{decimals}";
        return new TickFormatter(value => value.ToString(format, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 自定义格式化规则。
    /// </summary>
    public static TickFormatter Custom(Func<float, string> formatter)
    {
        return new TickFormatter(formatter);
    }
}
