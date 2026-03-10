using Godot;

/// <summary>
/// 单位的原子级碰撞定义
/// 与 AreaCollision 的区别在于它永远只占用一个地格，因此不再需要 Width 和 Height
/// </summary>
[Persistable]
public class AtomCollision
{
	[PersistField]
	private Vector2I center;

	/// <summary>
	/// 原子碰撞体的中心点坐标，使用地格坐标表示
	/// </summary>
	public Vector2I Center => center;

	/// <summary>
	/// 网格中心的坐标 (Vector2)
	/// 对于 1x1 而言，其物理中心是 cell 中心，即 (X + 0.5, Y + 0.5)
	/// </summary>
	public Vector2 CenterAtGrid => new Vector2(center.X + 0.5f, center.Y + 0.5f);

	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private AtomCollision()
	{
	}

	/// <summary>
	/// 构造函数，初始化中心点
	/// </summary>
	/// <param name="center">初始中心点坐标</param>
	public AtomCollision(Vector2I center)
	{
		this.center = center;
	}
}

// CenterF = new Vector2(center.X, center.Y);

// /// <summary>
// /// 原子碰撞体的浮点型中心点坐标，用于执行移动动画。不需要持久化。
// /// </summary>
// public Vector2 CenterF { get; set; }


// /// <summary>
// /// 浮动网格中心的坐标 (Vector2)
// /// </summary>
// public Vector2 CenterFAtGrid => new Vector2(CenterF.X + 0.5f, CenterF.Y + 0.5f);