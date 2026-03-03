

/// <summary>
/// 世界逻辑接口，定义世界级别的持续性逻辑处理
/// </summary>
public interface IWorldLogic
{
    /// <summary>
    /// 世界逻辑名称（短文本）
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 世界逻辑描述（固定文本）
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 每帧调用，处理内部计时器逻辑
    /// </summary>
    /// <param name="delta">时间增量</param>
    void UpdateDelta(float delta);

    /// <summary>
    /// 获取触发进度（0.0 - 1.0）
    /// </summary>
    float GetProgressRatio();
}