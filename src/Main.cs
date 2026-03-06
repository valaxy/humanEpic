using Godot;

/// <summary>
/// 3D 场景主流程入口。
/// 当前为最小可运行版本：负责接入摄像机、GameView 与网格辅助线。
/// 其余未完成模块先保留注释占位，后续可逐步补充。
/// </summary>
[GlobalClass]
public partial class Main : Node3D
{
	// 主视图场景。
	private static readonly PackedScene gameViewScene = GD.Load<PackedScene>("res://src/view/main_view.tscn");

	// 主摄像机节点。
	private GameCamera camera = null!;

	// 视图层顶层入口。
	private GameView view = null!;
	// 领域层顶层入口。
	private GameWorld world = null!;
	// 世界逻辑模拟入口。
	private Simulation simulation = null!;
	// 主 UI 管理器。
	private MainUI mainUi = null!;

	public override void _Ready()
	{
		camera = GetNode<GameCamera>("Camera3D");
		world = GameWorldInitializer.Load();
		simulation = new Simulation(world);

		addCoreNodes();
		view.Setup(camera, world);
		setupUi();
	}

	public override void _Process(double delta)
	{
		world.TimeSystem.Update(delta);
		simulation.Update(delta);
	}

	// 添加主流程核心节点。
	private void addCoreNodes()
	{
		view = gameViewScene.Instantiate<GameView>();
		AddChild(view);
	}



	// 初始化并挂载主 UI。
	private void setupUi()
	{
		PackedScene mainUiScene = GD.Load<PackedScene>("res://src/ui/main_ui.tscn");
		mainUi = mainUiScene.Instantiate<MainUI>();
		AddChild(mainUi);
		mainUi.Setup(world, view, simulation, camera, view.LayerManager, view.GroundView);
	}
}