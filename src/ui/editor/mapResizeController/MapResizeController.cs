using Godot;

/// <summary>
/// 地图尺寸修改控制器。
/// 提供地图宽高输入与确认回调。
/// </summary>
[GlobalClass]
public partial class MapResizeController : HBoxContainer
{
	// 引擎支持的最大地图逻辑宽度。
	private const int MaxWidth = 2000;
	// 引擎支持的最大地图逻辑高度。
	private const int MaxHeight = 2000;

	// 宽度数值输入框。
	private SpinBox mapWidthSpinBox = null!;
	// 高度数值输入框。
	private SpinBox mapHeightSpinBox = null!;
	// 触发尺寸重设的确认按钮。
	private Button confirmResizeButton = null!;
	// 地理逻辑层引用。
	private Ground ground = null!;

	public override void _Ready()
	{
		mapWidthSpinBox = GetNode<SpinBox>("%MapWidthSpinBox");
		mapHeightSpinBox = GetNode<SpinBox>("%MapHeightSpinBox");
		confirmResizeButton = GetNode<Button>("%ConfirmResizeButton");

		mapWidthSpinBox.MaxValue = MaxWidth;
		mapHeightSpinBox.MaxValue = MaxHeight;
		confirmResizeButton.Pressed += onConfirmResizeButtonPressed;
	}

	/// <summary>
	/// 初始化控制器并绑定地理逻辑层。
	/// </summary>
	public void Setup(Ground ground)
	{
		this.ground = ground;
		mapWidthSpinBox.SetValueNoSignal(Mathf.Clamp(ground.Width, 1, MaxWidth));
		mapHeightSpinBox.SetValueNoSignal(Mathf.Clamp(ground.Height, 1, MaxHeight));
	}

	// 响应确认按钮点击并触发地图尺寸重设。
	private void onConfirmResizeButtonPressed()
	{
		int width = (int)mapWidthSpinBox.Value;
		int height = (int)mapHeightSpinBox.Value;
		ground.Resize(width, height);
	}
}
