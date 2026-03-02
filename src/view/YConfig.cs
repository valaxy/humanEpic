using Godot;

/// <summary>
/// 深度/高度配置，统一管理所有物件在 Y 轴（纵深）上的高度，以处理遮挡关系
/// </summary>
[GlobalClass]
public partial class YConfig : RefCounted
{
	// --- 基础层面 ---
	/// <summary>地面模型基础高度</summary>
	public static float GroundY => 0.0f;
	/// <summary>地表平面的高度（策略层的地块渲染）</summary>
	public static float SurfaceY => 0.1f;
	/// <summary>网格线高度，略高于地表高度</summary>
	public static float GridHelperY => SurfaceY + 0.01f;
	/// <summary>地格指示器高度，辅助用的，略高于网格线</summary>
	public static float GridIndicatorY => SurfaceY + 0.02f;


	// --- 地形高度层 ---
	/// <summary>不同地高之间的垂直位移</summary>
	public static float HeightDisplacement => 0.3f;
	/// <summary>平原高度</summary>
	public static float PlainY => 0.1f;
	/// <summary>丘陵高度</summary>
	public static float HillY => 0.4f;
	/// <summary>山地高度</summary>
	public static float MountainY => 1.2f;
	/// <summary>地形地格底部的统一高度</summary>
	public static float TerrainBottomY => -0.1f;

	// --- 交互与反馈层 ---
	/// <summary>单位阴影高度，略高于地面</summary>
	public static float ShadowY => 0.02f;

	/// <summary>鼠标悬停光标高度，略高于地表</summary>
	public static float CursorY => 0.12f;

	/// <summary>选中框高度，略高于光标以防遮挡</summary>
	public static float SelectionY => 0.15f;


	// --- 物体与单位层 ---
	/// <summary>覆盖物渲染的垂直偏移</summary>
	public static float OverlayYOffset => 0.1f;




	
	/// <summary>单位基础 Y 偏移（相对于地表的高度，如果 pivot 在中心则为高度的一半）</summary>
	public static float UnitYOffset => 0.0f;

	/// <summary>移动路径线高度偏移（相对于地表）</summary>
	public static float UnitPathLineYOffset => 0.02f;

	// --- 渲染优先级 (RenderPriority) ---
	/// <summary>网格辅助线渲染优先级</summary>
	public static int GridRenderPriority => 10;
	/// <summary>战略层图层渲染优先级</summary>
	public static int StrategyLayerRenderPriority => 1;
}
