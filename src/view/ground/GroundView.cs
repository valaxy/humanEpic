using Godot;

/// <summary>
/// 地面视图入口，负责地面数据与地面渲染的初始化与同步。
/// </summary>
[GlobalClass]
public partial class GroundView : Node3D
{
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

	// 最近一次悬浮的格点。
	private Vector2I lastHoveredCell = Vector2I.Zero;

	// 是否存在有效悬浮格点。
	private bool hasHoveredCell;

	// 地图笔刷。
	private Brush brush = null!;

	// 预览容器。
	private Node3D previewRoot = null!;

	// 地图编辑器。
	private GroundEditor groundEditor = null!;


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
	/// 一次连续绘制（按下到抬起）完成时发出。
	/// </summary>
	[Signal]
	public delegate void DrawCompletedEventHandler();


	/// <summary>
	/// 获取笔刷节点。
	/// </summary>
	public Brush GetBrush()
	{
		return brush;
	}

	/// <summary>
	/// 获取地面编辑器节点。
	/// </summary>
	public GroundEditor GetEditor()
	{
		return groundEditor;
	}

	public override void _Ready()
	{
		brush = GetNode<Brush>("Brush");
		brush.Visible = false;
		previewRoot = GetNode<Node3D>("BrushPreviewRoot");
		groundEditor = GetNode<GroundEditor>("GroundEditor");
	}

	/// <summary>
	/// 绑定地面初始化所需依赖。
	/// </summary>
	public void Setup(Ground ground, LayerManagerNode layerManager, GroundGridHelper gridRender, GameCamera camera)
	{
		this.ground = ground;
		this.layerManager = layerManager;
		this.gridRender = gridRender;
		this.camera = camera;
		groundEditor.Setup(ground, brush, previewRoot);
		hasHoveredCell = false;

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
					GetViewport().SetInputAsHandled();
					groundEditor.BeginDraw();
				}
			}
			else
			{
				if (groundEditor.EndDraw())
				{
					EmitSignal(SignalName.DrawCompleted);
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		// 更新地面交互状态与笔刷可视。
		if (GetViewport().GuiGetHoveredControl() != null)
		{
			brush.Visible = false;
			groundEditor.HidePreviewAndForbidden();
			return;
		}

		Vector3? worldPoint = camera.GetRayIntersection(YConfig.PlainY);
		if (!worldPoint.HasValue)
		{
			brush.Visible = false;
			groundEditor.HidePreviewAndForbidden();
			return;
		}

		Vector2I cellPos = ground.WorldToCell(worldPoint.Value);
		handleMouseHover(GetViewport().GetMousePosition());

		if (!groundEditor.IsDrawableCell(cellPos))
		{
			brush.Visible = false;
			groundEditor.HidePreviewAndForbidden();
			return;
		}

		brush.Visible = true;
		groundEditor.UpdateCursorVisual(cellPos);
		groundEditor.UpdatePreview(cellPos);

		if (groundEditor.TryConsumeDrawCell(cellPos))
		{
			EmitSignal(SignalName.CellClicked, cellPos);
		}
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
}