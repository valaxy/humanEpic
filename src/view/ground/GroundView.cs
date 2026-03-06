using Godot;

/// <summary>
/// 地面视图入口，负责地面数据与地面渲染的初始化与同步。
/// </summary>
[GlobalClass]
public partial class GroundView : Node3D
{
	private static readonly PackedScene gridCursorScene = GD.Load<PackedScene>("res://src/view/ground/gridCursor/GridCursor.tscn");

	/// <summary>默认地图宽度</summary>
	[Export]
	public int DefaultMapWidth { get; set; } = 500;

	/// <summary>默认地图高度</summary>
	[Export]
	public int DefaultMapHeight { get; set; } = 500;

	// 摄像机引用，用于屏幕坐标投影。
	private GameCamera camera = null!;
	// 地形数据模型引用。
	private Ground ground = null!;
	// 地图层级渲染管理器引用。
	private LayerManagerNode layerManager = null!;
	// 网格辅助线渲染引用。
	private GroundGridHelper gridRender = null!;
	// 世界模型引用。
	private GameWorld world = null!;
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
	public void Setup(GameWorld world, LayerManagerNode layerManager, GroundGridHelper gridRender, GameCamera camera, BuildingCollectionView buildingCollectionView)
	{
		this.world = world;
		ground = world.Ground;
		this.layerManager = layerManager;
		this.gridRender = gridRender;
		this.camera = camera;
		this.buildingCollectionView = buildingCollectionView;
		hasHoveredCell = false;

		gridCursor = gridCursorScene.Instantiate<GridCursor>();
		AddChild(gridCursor);
		buildingCollectionView.BuildingClicked += onBuildingClicked;

		initializeMap(DefaultMapWidth, DefaultMapHeight);
	}

	// 初始化地图尺寸并同步初始渲染。
	private void initializeMap(int width, int height)
	{
		ground.Resize(width, height);
		layerManager.UpdateMapData(ground);
		gridRender.UpdateGrid(width, height);
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
			if (mouseButton.Pressed)
			{
				if (camera.TryResolveGroundCell(mouseButton.Position, ground, out Vector2I cellPos, YConfig.PlainY))
				{
					EmitSignal(SignalName.CellClicked, cellPos);
					processSelection(cellPos);
					// 注释暂时保留 
					// GetViewport().SetInputAsHandled();
					EmitSignal(SignalName.EditPrimaryPressed, cellPos);
				}
			}
			else
			{
				EmitSignal(SignalName.EditPrimaryReleased);
			}
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

		Vector3? worldPoint = camera.GetRayIntersection(YConfig.PlainY);
		if (!worldPoint.HasValue)
		{
			clearHoverIfNeeded();
			EmitSignal(SignalName.EditPointerCellCleared);
			return;
		}

		Vector2I cellPos = ground.WorldToCell(worldPoint.Value);
		handleMouseHover(GetViewport().GetMousePosition());
		EmitSignal(SignalName.EditPointerCellChanged, cellPos);
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
			if (hasHoveredCell)
			{
				hasHoveredCell = false;
				EmitSignal(SignalName.CellHoverCleared);
			}

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

	// 处理建筑点击行为。
	private void onBuildingClicked(Vector2I cellPos)
	{
		processSelection(cellPos);
	}

	// 处理地格选中状态流转。
	private void processSelection(Vector2I cellPos)
	{
		if (!ground.IsInsideGround(cellPos))
		{
			ClearSelection();
			return;
		}

		ShowCellSelection(cellPos);
		EmitSignal(SignalName.CellSelected, cellPos);

		if (world.Buildings.HasKey(cellPos))
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