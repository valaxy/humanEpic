using Godot;
/// <summary>
/// 渲染层的抽象基类，定义层级管理的基础接口
/// </summary>
public abstract partial class LayerNode : Node3D
{
    /// <summary>
    /// 设置层的透明度，用于多层视图平滑切换
    /// </summary>
    /// <param name="alpha">透明度 (0.0 - 1.0)</param>
    public abstract void SetAlpha(float alpha);

    /// <summary>
    /// 全量更新层级数据
    /// </summary>
    /// <param name="ground">地形数据模型</param>
    public abstract void UpdateLayer(Ground ground);

    /// <summary>
    /// 增量更新指定地格的渲染表现
    /// </summary>
    /// <param name="cells">发生变化的地格坐标集合</param>
    /// <param name="ground">地形数据模型</param>
    public virtual void UpdateCells(Godot.Collections.Array<Vector2I> cells, Ground ground)
    {
        // 默认实现为全量更新，子类可根据需要进行性能优化
        UpdateLayer(ground);
    }
}
