using Godot;

/// <summary>
/// 地格，组成地平面的单元格
/// </summary>
public class Grid
{
	private Overlay overlay = null!;
	private SurfaceTemplate surface = null!;

	/// <summary>
	/// 当前地表
	/// </summary>
	public SurfaceType.Enums SurfaceType => surface.Type;

	/// <summary>
	/// 当前覆盖物对象
	/// </summary>
	public Overlay Overlay => overlay;

	/// <summary>
	/// 当前覆盖物类型
	/// </summary>
	public OverlayType.Enums OverlayType => overlay.Type;

	/// <summary>
	/// 地格被国家标记后的颜色（无标记时为空）
	/// </summary>
	public Color? CountryColor { get; set; }


	/// <summary>
	/// 初始化地格
	/// </summary>
	/// <param name="surfaceType">初始地表类型</param>
	/// <param name="overlay">初始覆盖物类型</param>
	/// <param name="heightInit">初始地高类型</param>
	public Grid(SurfaceType.Enums surfaceType, Overlay overlay)
	{
		UpdateSurface(surfaceType);
		this.overlay = overlay;
	}


	/// <summary>
	/// 更新地表类型
	/// </summary>
	public void UpdateSurface(SurfaceType.Enums newSurface)
	{
		surface = SurfaceTemplate.GetTemplate(newSurface);
	}


	/// <summary>
	/// 更新覆盖物类型
	/// </summary>
	public void UpdateOverlay(OverlayType.Enums newOverlay)
	{
		// 复用None，优化性能
		overlay = newOverlay == global::OverlayType.Enums.NONE ? Overlay.None : new Overlay(newOverlay);
	}
}
