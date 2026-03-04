using Godot;

/// <summary>
/// MapResizeUI API 演示入口。
/// </summary>
[GlobalClass]
public partial class MapResizeUIDemo : Control
{
	// 地图尺寸 UI 组件。
	private MapResizeUI mapResizeUi = null!;
	// 状态显示文本。
	private Label statusLabel = null!;
	// 设置 128x96 的测试按钮。
	private Button setPresetButton = null!;
	// 刷新当前尺寸显示按钮。
	private Button readButton = null!;
	// 地理管理器模拟节点。
	private MapResizeDemoGeographyManager geographyManager = null!;

	public override void _Ready()
	{
		mapResizeUi = GetNode<MapResizeUI>("%MapResizeUI");
		statusLabel = GetNode<Label>("%StatusLabel");
		setPresetButton = GetNode<Button>("%SetPresetButton");
		readButton = GetNode<Button>("%ReadButton");

		geographyManager = new MapResizeDemoGeographyManager();
		AddChild(geographyManager);

		mapResizeUi.ResizeConfirmed += onResizeConfirmed;
		setPresetButton.Pressed += onSetPresetPressed;
		readButton.Pressed += refreshStatus;

		mapResizeUi.InitializeValues();
		refreshStatus();
	}

	// 响应 UI 确认回调。
	private void onResizeConfirmed(int width, int height)
	{
		statusLabel.Text = $"确认回调：{width} x {height}";
	}

	// 通过 API 主动写入演示值。
	private void onSetPresetPressed()
	{
		mapResizeUi.SetMapSize(128, 96);
		refreshStatus();
	}

	// 读取 UI 当前值并更新状态展示。
	private void refreshStatus()
	{
		int width = mapResizeUi.GetMapWidth();
		int height = mapResizeUi.GetMapHeight();
		statusLabel.Text = $"当前输入值：{width} x {height}";
	}
}

/// <summary>
/// 地图尺寸演示用地理管理器。
/// </summary>
[GlobalClass]
public partial class MapResizeDemoGeographyManager : Node
{
	/// <summary>
	/// 演示用地图数据对象。
	/// </summary>
	public MapResizeDemoGround Ground { get; } = new MapResizeDemoGround();

	public override void _Ready()
	{
		AddToGroup("geography_manager");
	}

	/// <summary>
	/// 演示用地图重设接口。
	/// </summary>
	public void resize_map(int width, int height)
	{
		Ground.Width = width;
		Ground.Height = height;
	}
}

/// <summary>
/// 地图尺寸演示用地表数据。
/// </summary>
[GlobalClass]
public partial class MapResizeDemoGround : RefCounted
{
	/// <summary>
	/// 地图宽度。
	/// </summary>
	public int Width { get; set; } = 50;

	/// <summary>
	/// 地图高度。
	/// </summary>
	public int Height { get; set; } = 50;
}
