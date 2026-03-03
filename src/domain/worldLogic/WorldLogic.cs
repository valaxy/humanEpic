using System;

/// <summary>
/// 世界逻辑抽象类：负责按间隔天数触发世界逻辑。
/// </summary>
public abstract class WorldLogic : IWorldLogic
{
    // 已累计的天数。
    private float elapsedDays;

    // 执行间隔（天）。
    private float intervalDays;

    /// <summary>
    /// 世界逻辑名称（短文本）。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 世界逻辑描述（固定文本）。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 执行间隔（天），小于等于 0 表示每次 UpdateDelta 都触发。
    /// </summary>
    public float IntervalDays => intervalDays;

    /// <summary>
    /// 初始化世界逻辑。
    /// </summary>
    /// <param name="name">逻辑名称。</param>
    /// <param name="description">逻辑描述。</param>
    /// <param name="intervalDays">触发间隔天数。</param>
    protected WorldLogic(string name, string description, float intervalDays)
    {
        Name = name;
        Description = description;
        this.intervalDays = intervalDays;
        elapsedDays = 0.0f;
    }

    /// <summary>
    /// 每帧调用，处理内部计时器逻辑。
    /// </summary>
    /// <param name="delta">时间增量（秒）。</param>
    public void UpdateDelta(float delta)
    {
        if (intervalDays <= 0.0f)
        {
            ProcessLogic();
            return;
        }

        elapsedDays += delta / TimeSystem.SecondsPerDay;
        while (elapsedDays >= intervalDays)
        {
            elapsedDays -= intervalDays;
            ProcessLogic();
        }
    }

    /// <summary>
    /// 获取触发进度（0.0 - 1.0）。
    /// </summary>
    public float GetProgressRatio()
    {
        if (intervalDays <= 0.0f)
        {
            return 1.0f;
        }

        return Math.Clamp(elapsedDays / intervalDays, 0.0f, 1.0f);
    }

    /// <summary>
    /// 逻辑触发时执行的业务逻辑。
    /// </summary>
    protected abstract void ProcessLogic();
}
