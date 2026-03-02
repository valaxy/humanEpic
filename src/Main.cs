using Godot;

/// <summary>
/// 3D 场景主流程入口。
/// 当前为最小可运行版本：负责接入摄像机、GameView 与网格辅助线。
/// 其余未完成模块先保留注释占位，后续可逐步补充。
/// </summary>
[GlobalClass]
public partial class Main : Node3D
{
	/// <summary>默认地图宽度</summary>
	[Export]
	public int DefaultMapWidth { get; set; } = 500;

	/// <summary>默认地图高度</summary>
	[Export]
	public int DefaultMapHeight { get; set; } = 500;

	// 主摄像机节点。
	private GameCamera camera = null!;
	
	// 视图层顶层入口。
	private GameView view = new GameView();

	public override void _Ready()
	{
		camera = GetNode<GameCamera>("Camera3D");

		addCoreNodes();
		setupView();
		initializeGridVisuals();

		// TODO: 后续逐步接回领域层与 UI 主流程。
		// GameWorld world = new GameWorld();
		// NaturalDisasterManager naturalDisasterManager = new NaturalDisasterManager();
		// MainUI mainUi = new MainUI();
	}

	// 添加主流程核心节点。
	private void addCoreNodes()
	{
		AddChild(view);
	}

	// 初始化视图层。
	private void setupView()
	{
		view.Setup(camera);
	}

	// 根据默认地图尺寸初始化网格渲染。
	private void initializeGridVisuals()
	{
		view.UpdateGridVisuals(DefaultMapWidth, DefaultMapHeight);
	}
}
