using Godot;

/// <summary>
/// 地面视图入口，负责地面数据与地面渲染的初始化与同步。
/// </summary>
[GlobalClass]
public partial class GroundView : Node
{
	// 摄像机引用，用于屏幕坐标投影。
	private GameCamera camera = null!;

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

	/// <summary>地形数据模型引用。</summary>
	public Ground Ground { get; private set; } = null!;

	/// <summary>地图层级渲染管理器引用。</summary>
	public LayerManagerNode LayerManager { get; private set; } = null!;

	/// <summary>网格辅助线渲染引用。</summary>
	public GroundGridHelper GridRender { get; private set; } = null!;

	/// <summary>
	/// 绑定地面初始化所需依赖。
	/// </summary>
	public void Setup(Ground ground, LayerManagerNode layerManager, GroundGridHelper gridRender, GameCamera camera)
	{
		Ground = ground;
		LayerManager = layerManager;
		GridRender = gridRender;
		this.camera = camera;
		hasHoveredCell = false;
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

	/// <summary>
	/// 处理用户在地面上的悬浮和点击行为。
	/// </summary>
	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseMotion mouseMotion)
		{
			handleMouseHover(mouseMotion.Position);
			return;
		}

		if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButton)
		{
			handleMouseClick(mouseButton.Position);
		}
	}

	// 处理鼠标悬浮行为。
	private void handleMouseHover(Vector2 screenPos)
	{
		if (!tryResolveCell(screenPos, out Vector2I cellPos))
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

	// 处理鼠标点击行为。
	private void handleMouseClick(Vector2 screenPos)
	{
		if (tryResolveCell(screenPos, out Vector2I cellPos))
		{
			EmitSignal(SignalName.CellClicked, cellPos);
			GetViewport().SetInputAsHandled();
		}
	}

	// 将屏幕坐标解析为地格坐标。
	private bool tryResolveCell(Vector2 screenPos, out Vector2I cellPos)
	{
		cellPos = Vector2I.Zero;

		Vector3? worldPoint = camera.ProjectToPlane(screenPos, YConfig.PlainY);
		if (!worldPoint.HasValue)
		{
			return false;
		}

		Vector2 cellFloat = Ground.WorldToGrid(worldPoint.Value);
		Vector2I resolved = new Vector2I(Mathf.FloorToInt(cellFloat.X), Mathf.FloorToInt(cellFloat.Y));
		if (!Ground.IsInsideGround(resolved))
		{
			return false;
		}

		cellPos = resolved;
		return true;
	}
}