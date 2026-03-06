using Godot;

/// <summary>
/// 地面视图入口，负责地面数据与地面渲染的初始化与同步。
/// </summary>
[GlobalClass]
public partial class GroundView : Node3D
{
	private static readonly PackedScene gridCursorScene = GD.Load<PackedScene>("res://src/view/ground/gridCursor/GridCursor.tscn");

	// 默认地图宽度
	private readonly int defaultMapWidth = 500;
	// 默认地图高度
	private readonly int defaultMapHeight = 500;



	// 建筑物集合
	private BuildingCollection buildings = null!;
	// 地形数据模型引用。
	private Ground ground = null!;



	// 摄像机引用，用于屏幕坐标投影。
	private GameCamera camera = null!;
	// 地图层级渲染管理器引用。
	private LayerManagerNode layerManager = null!;
	// 网格辅助线渲染引用。
	private GroundGridHelper gridRender = null!;
	// 建筑集合渲染视图。
	private BuildingCollectionView buildingCollectionView = null!;
	// 地格选中光标。
	private GridCursor gridCursor = null!;
	// 最近一次悬浮的格点。
	private Vector2I lastHoveredCell = Vector2I.Zero;
	// 是否存在有效悬浮格点。
	private bool hasHoveredCell;


	/// <summary>
	/// 鼠标悬浮在有效地格时发出。
	/// </summary>
	[Signal]
	public delegate void CellHoveredEventHandler(Vector2I cellPos);

	/// <summary>
	/// 鼠标悬浮离开有效地格时发出。
	/// </summary>
	[Signal]
	public delegate void CellHoverClearedEventHandler();

	/// <summary>
	/// 鼠标左键点击有效地格时发出。
	/// </summary>
	[Signal]
	public delegate void CellClickedEventHandler(Vector2I cellPos);

	/// <summary>
	/// 地格选中时发出。
	/// </summary>
	[Signal]
	public delegate void CellSelectedEventHandler(Vector2I cellPos);

	/// <summary>
	/// 选中状态清理时发出。
	/// </summary>
	[Signal]
	public delegate void SelectionClearedEventHandler();

	/// <summary>
	/// 建筑被选中时发出。
	/// </summary>
	[Signal]
	public delegate void BuildingSelectedEventHandler(Vector2I cellPos);

	/// <summary>
	/// 建筑选中清理时发出。
	/// </summary>
	[Signal]
	public delegate void BuildingSelectionClearedEventHandler();

	/// <summary>
	/// 主指针命中地格时发出。
	/// </summary>
	[Signal]
	public delegate void EditPointerCellChangedEventHandler(Vector2I cellPos);

	/// <summary>
	/// 主指针离开地面有效投影时发出。
	/// </summary>
	[Signal]
	public delegate void EditPointerCellClearedEventHandler();

	/// <summary>
	/// 主指针左键按下且命中地格时发出。
	/// </summary>
	[Signal]
	public delegate void EditPrimaryPressedEventHandler(Vector2I cellPos);

	/// <summary>
	/// 主指针左键抬起时发出。
	/// </summary>
	[Signal]
	public delegate void EditPrimaryReleasedEventHandler();

	/// <summary>
	/// 绑定地面初始化所需依赖。
	/// </summary>
	public void Setup(BuildingCollection buildings, Ground ground,
		LayerManagerNode layerManager, GroundGridHelper gridRender, GameCamera camera, BuildingCollectionView buildingCollectionView)
	{
		this.buildings = buildings;
		this.ground = ground;

		this.layerManager = layerManager;
		this.gridRender = gridRender;
		this.camera = camera;
		this.buildingCollectionView = buildingCollectionView;
		hasHoveredCell = false;

		gridCursor = gridCursorScene.Instantiate<GridCursor>();
		AddChild(gridCursor);
		buildingCollectionView.BuildingClicked += onBuildingClicked;

		initializeMap(defaultMapWidth, defaultMapHeight);
	}

	// 初始化地图尺寸并同步初始渲染。
	private void initializeMap(int width, int height)
	{
		ground.Resize(width, height);
		layerManager.UpdateMapData(ground);
		gridRender.UpdateGrid(width, height);
	}

	// 处理建筑节点触发的选中流程。
	private void onBuildingClicked(Vector2I cellPos)
	{
		if (!ground.IsInsideGround(cellPos))
		{
			ClearSelection();
			return;
		}

		EmitSignal(SignalName.BuildingSelected, cellPos);
	}

	/// <summary>
	/// 处理用户在地面上的悬浮和点击行为。
	/// </summary>
	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (GetViewport().GuiGetHoveredControl() != null)
		{
			return;
		}

		if (inputEvent is InputEventMouseMotion mouseMotion)
		{
			handleMouseHover(mouseMotion.Position);
			return;
		}

		if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton)
		{
			handlePrimaryMouseButton(mouseButton);
		}
	}

	public override void _Process(double delta)
	{
		if (GetViewport().GuiGetHoveredControl() != null)
		{
			clearHoverIfNeeded();
			EmitSignal(SignalName.EditPointerCellCleared);
			return;
		}

		Vector2 mousePos = GetViewport().GetMousePosition();
		handleMouseHover(mousePos);

		if (!camera.TryResolveGroundCell(mousePos, ground, out Vector2I cellPos, YConfig.PlainY))
		{
			EmitSignal(SignalName.EditPointerCellCleared);
			return;
		}

		EmitSignal(SignalName.EditPointerCellChanged, cellPos);
	}

	// 处理主鼠标按键事件。
	private void handlePrimaryMouseButton(InputEventMouseButton mouseButton)
	{
		if (!mouseButton.Pressed)
		{
			EmitSignal(SignalName.EditPrimaryReleased);
			return;
		}

		if (!camera.TryResolveGroundCell(mouseButton.Position, ground, out Vector2I cellPos, YConfig.PlainY))
		{
			return;
		}

		EmitSignal(SignalName.CellClicked, cellPos);
		selectGroundCell(cellPos);
		EmitSignal(SignalName.EditPrimaryPressed, cellPos);
	}

	// 清理悬浮状态并发出清理信号。
	private void clearHoverIfNeeded()
	{
		if (!hasHoveredCell)
		{
			return;
		}

		hasHoveredCell = false;
		EmitSignal(SignalName.CellHoverCleared);
	}

	// 处理鼠标悬浮行为。
	private void handleMouseHover(Vector2 screenPos)
	{
		if (!camera.TryResolveGroundCell(screenPos, ground, out Vector2I cellPos, YConfig.PlainY))
		{
			clearHoverIfNeeded();
			return;
		}

		if (hasHoveredCell && cellPos == lastHoveredCell)
		{
			return;
		}

		hasHoveredCell = true;
		lastHoveredCell = cellPos;
		EmitSignal(SignalName.CellHovered, cellPos);
	}



	// 处理地格选中状态流转。
	private void selectGroundCell(Vector2I cellPos)
	{
		if (!ground.IsInsideGround(cellPos))
		{
			ClearSelection();
			return;
		}

		ShowCellSelection(cellPos);
		EmitSignal(SignalName.CellSelected, cellPos);

		if (buildings.HasKey(cellPos))
		{
			EmitSignal(SignalName.BuildingSelected, cellPos);
			return;
		}

		buildingCollectionView.Unselect();
		EmitSignal(SignalName.BuildingSelectionCleared);
	}



	/// <summary>
	/// 显示地格选中光标。
	/// </summary>
	public void ShowCellSelection(Vector2I cellPos)
	{
		gridCursor.ShowCell(cellPos, ground);
	}

	/// <summary>
	/// 清理所有选中状态。
	/// </summary>
	public void ClearSelection()
	{
		gridCursor.Clear();
		buildingCollectionView.Unselect();
		EmitSignal(SignalName.SelectionCleared);
		EmitSignal(SignalName.BuildingSelectionCleared);
	}
}