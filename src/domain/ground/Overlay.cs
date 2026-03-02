using Godot;

/// <summary>
/// 地格覆盖物类，处理覆盖物的数据和基础逻辑
/// 覆盖物的属性定义在 OverlayTemplate 中
/// </summary>
public class Overlay
{
	/// <summary>
	/// 共享的空覆盖物实例，用于优化内存和加载速度
	/// </summary>
	public static readonly Overlay None = new Overlay(OverlayType.Enums.NONE, 0.0f);


	/// <summary>
	/// 当前资源量
	/// </summary>
	public float Amount { get; }

	/// <summary>
	/// 关联的模板
	/// </summary>
	public OverlayTemplate Template { get; }


	/// <summary>
	/// 覆盖物类型
	/// </summary>
	public OverlayType.Enums Type => Template.Type;

	public string Name => Template.Name;

	public Color Color => Template.Color;

	public float MaxAmount => Template.MaxAmount;

	public float AmountRatio => Amount / MaxAmount;



	/// <summary>
	/// 初始化覆盖物实例
	/// </summary>
	/// <param name="type">覆盖物类型</param>
	/// <param name="amount">当前资源产量</param>
	public Overlay(OverlayType.Enums type)
	{
		Template = OverlayTemplate.GetTemplate(type);
		Amount = Template.MaxAmount; // 默认等于最大值
	}

	public Overlay(OverlayType.Enums type, float amount)
	{
		Template = OverlayTemplate.GetTemplate(type);
		Amount = amount;
	}


	/// <summary>
	/// 检查当前覆盖物是否可以放置在指定的地表上
	/// </summary>
	public bool IsValidOn(SurfaceType.Enums surface) => OverlayTemplate.IsValid(surface, Type);
}
