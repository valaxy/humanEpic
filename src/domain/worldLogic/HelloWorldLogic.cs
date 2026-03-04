/// <summary>
/// 示例世界逻辑：用于验证逻辑注册与 UI 展示链路，不执行任何业务。
/// </summary>
public class HelloWorldLogic : WorldLogic
{
    /// <summary>
    /// 初始化一个什么事都不干的世界逻辑。
    /// </summary>
    public HelloWorldLogic() : base("HelloWorldLogic", "示例逻辑：仅占位，不执行任何行为。", 1.0f)
    {
    }

    /// <summary>
    /// 逻辑触发时的处理函数（空实现）。
    /// </summary>
    protected override void ProcessLogic()
    {
    }
}
