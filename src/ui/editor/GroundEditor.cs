using Godot;

/// <summary>
/// 地图编辑器状态对象。
/// 用于统一存储编辑器运行时属性。
/// </summary>
[GlobalClass]
public partial class GroundEditor : Node
{
	/// <summary>
	/// 当笔刷大小发生变化时发出。
	/// </summary>
	[Signal]
	public delegate void BrushSizeChangedEventHandler(int brushSize);

	// 笔刷大小最小值。
	private const int MinBrushSize = 1;
	// 笔刷大小最大值。
	private const int MaxBrushSize = 10;

	/// <summary>
	/// 当前笔刷大小。
	/// </summary>
	public int BrushSize { get; private set; } = 1;

	/// <summary>
	/// 当前选中的地表类型。
	/// </summary>
	public SurfaceType.Enums SurfaceType { get; private set; } = global::SurfaceType.Enums.GRASSLAND;

	/// <summary>
	/// 当前选中的覆盖物类型。
	/// </summary>
	public OverlayType.Enums OverlayType { get; private set; } = global::OverlayType.Enums.NONE;


	/// <summary>
	/// 设置笔刷大小。
	/// </summary>
	public void SetBrushSize(int value)
	{
		int clampedValue = Mathf.Clamp(value, MinBrushSize, MaxBrushSize);
		if (clampedValue == BrushSize)
		{
			return;
		}

		BrushSize = clampedValue;
		EmitSignal(SignalName.BrushSizeChanged, BrushSize);
	}

	/// <summary>
	/// 设置地表类型。
	/// </summary>
	public void SetSurfaceType(SurfaceType.Enums value)
	{
		SurfaceType = value;
	}

	/// <summary>
	/// 设置覆盖物类型。
	/// </summary>
	public void SetOverlayType(OverlayType.Enums value)
	{
		OverlayType = value;
	}

}
