using Godot;

/// <summary>
/// 地面视图入口，负责地面数据与地面渲染的初始化与同步。
/// </summary>
[GlobalClass]
public partial class GroundView : Node
{
	/// <summary>地形数据模型引用。</summary>
	public Ground Ground { get; private set; } = null!;

	/// <summary>地图层级渲染管理器引用。</summary>
	public LayerManagerNode LayerManager { get; private set; } = null!;

	/// <summary>网格辅助线渲染引用。</summary>
	public GroundGridHelper GridRender { get; private set; } = null!;

	/// <summary>
	/// 绑定地面初始化所需依赖。
	/// </summary>
	public void Setup(Ground ground, LayerManagerNode layerManager, GroundGridHelper gridRender)
	{
		Ground = ground;
		LayerManager = layerManager;
		GridRender = gridRender;
	}

	/// <summary>
	/// 初始化地图尺寸并同步初始渲染。
	/// </summary>
	public void InitializeMap(int width, int height)
	{
		Ground.Resize(width, height);
		LayerManager.UpdateMapData(Ground);
		GridRender.UpdateGrid(width, height);
	}
}