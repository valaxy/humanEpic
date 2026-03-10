using System;
using System.Diagnostics;

/// <summary>
/// 进度型信息条目。
/// </summary>
public class InfoProgressEntryData : InfoEntryData
{
    /// <summary>
    /// 进度值，范围通常在 0 到 1 之间。
    /// </summary>
    public float Progress { get; }

    /// <summary>
    /// 可选展示文本。
    /// </summary>
    public string ValueText { get; }

    /// <summary>
    /// 初始化进度型信息条目。
    /// </summary>
    /// <param name="progress">进度值。</param>
    /// <param name="valueText">展示文本。</param>
    /// <param name="allowProgressGreaterOne">是否允许传入大于 1 的进度值。若允许则会自动截断为 1。</param>
    public InfoProgressEntryData(float progress, string valueText = "", bool allowProgressGreaterOne = false)
    {
        float normalizedProgress = allowProgressGreaterOne ? MathF.Min(progress, 1f) : progress;
        Debug.Assert(normalizedProgress >= 0 && normalizedProgress <= 1, "Progress value should be between 0 and 1.");
        Progress = normalizedProgress;
        ValueText = valueText;
    }

    public override string ToText()
    {
        return String.Empty; // 进度条本身不直接展示文本
    }
}
