using Godot;

/// <summary>
/// 视图层顶层入口类，负责所有视图、渲染、交互相关 Node 的实例化、生命周期管理等等
/// </summary>
[GlobalClass]
public partial class GameView : Node3D
{
	/// <summary>管理地图显示层级的管理器</summary>
	public LayerManagerNode LayerManager { get; private set; } = null!;
	/// <summary>地面网格渲染实例</summary>
	public GroundGridHelper GridRender { get; private set; } = null!;
	/// <summary>地面视图入口</summary>
	public GroundView GroundView { get; private set; } = null!;
	/// <summary>地面编辑器</summary>
	public GroundEditView GroundEditor { get; private set; } = null!;
	/// <summary>建筑集合渲染节点</summary>
	public BuildingCollectionView BuildingCollection { get; private set; } = null!;

	public override void _Ready()
	{
		LayerManager = GetNode<LayerManagerNode>("LayerManager");
		GroundView = GetNode<GroundView>("GroundView");
		GridRender = GetNode<GroundGridHelper>("GroundGridHelper");
		GroundEditor = GetNode<GroundEditView>("GroundView/GroundEditor");
		BuildingCollection = GetNode<BuildingCollectionView>("BuildingCollection");
	}

	/// <summary>
	/// 初始化视图层所有组件
	/// </summary>
	/// <param name="camera">主摄像机引用</param>
	public void Setup(GameCamera camera, GameWorld world)
	{
		camera.ZoomChanged += (float zoomValue, float minZoom, float maxZoom) =>
		{
			LayerManager.HandleZoom(zoomValue);
		};

		BuildingCollection.Setup(world);
		GroundView.Setup(world.Buildings, world.Ground, LayerManager, GridRender, camera, BuildingCollection);
		GroundEditor.Setup(world.Ground, world.Buildings, GroundView);
	}
}
