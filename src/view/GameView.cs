using Godot;

/// <summary>
/// 视图层顶层入口类，负责所有视图、渲染、交互相关 Node 的实例化、生命周期管理和依赖注入。
/// GDScript 只需要持有此类的引用即可访问整个视图层。
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
	// /// <summary>管理地理数据和网格绘制的管理器</summary>
	// public GroundNode Ground { get; private set; } = new GroundNode();
	// /// <summary>管理单位及其业务逻辑/显示的控制器</summary>
	// public UnitController UnitController { get; private set; } = new UnitController();
	// /// <summary>管理全局选中逻辑的控制器</summary>
	// public GroundSelectionNode SelectionController { get; private set; } = new GroundSelectionNode();
	// /// <summary>建筑集合显示逻辑</summary>
	// public BuildingCollectionNode BuildingCollection { get; private set; } = new BuildingCollectionNode();
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

		GroundView.Setup(world, LayerManager, GridRender, camera, BuildingCollection);
		GroundEditor.Setup(world.Ground, world.Buildings, GroundView);
		BuildingCollection.Setup(world);

		// 1. 初始化基础显示组件
		// Ground.Setup(camera, world, SelectionController, LayerManager);
		// BuildingCollection.Setup(world);

		// // 2. 初始化交互控制器
		// UnitController.Setup(world, camera, world.Ground, SelectionController);
		// UnitController.RegisterUnitCollection(world.UnitCollection);
		// UnitController.RegisterUnitCollection(world.WildlifeCollection);

		// SelectionController.Setup(world, Ground, BuildingCollection, UnitController);
	}
	// /// <summary>
	// /// 更新网格渲染状态
	// /// </summary>
	// public void UpdateGridVisuals(int width, int height)
	// {
	// 	GridRender.UpdateGrid(width, height);
	// }
}
