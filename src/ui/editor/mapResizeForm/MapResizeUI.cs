using Godot;

/// <summary>
/// 地图尺寸修改组件。
/// 提供地图宽高输入与确认回调。
/// </summary>
[GlobalClass]
public partial class MapResizeUI : HBoxContainer
{
	/// <summary>
	/// 当用户确认地图尺寸变更时发出。
	/// </summary>
	[Signal]
	public delegate void ResizeConfirmedEventHandler(int width, int height);

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

	public override void _Ready()
	{
		mapWidthSpinBox = GetNode<SpinBox>("%MapWidthSpinBox");
		mapHeightSpinBox = GetNode<SpinBox>("%MapHeightSpinBox");
		confirmResizeButton = GetNode<Button>("%ConfirmResizeButton");

		mapWidthSpinBox.MaxValue = MaxWidth;
		mapHeightSpinBox.MaxValue = MaxHeight;
		confirmResizeButton.Pressed += onConfirmResizeButtonPressed;

		CallDeferred(MethodName.InitializeValues);
	}

	/// <summary>
	/// 初始化输入框中的初始地图行列数值。
	/// </summary>
	public void InitializeValues()
	{
		Node? geographyManager = GetTree().GetFirstNodeInGroup("geography_manager");
		if (geographyManager == null)
		{
			return;
		}

		Variant groundVariant = geographyManager.Get("Ground");
		if (groundVariant.VariantType == Variant.Type.Nil)
		{
			return;
		}

		GodotObject ground = groundVariant.AsGodotObject();
		int width = (int)ground.Get("Width");
		int height = (int)ground.Get("Height");
		SetMapSize(width, height);
	}

	/// <summary>
	/// 设置地图宽高到输入框。
	/// </summary>
	public void SetMapSize(int width, int height)
	{
		mapWidthSpinBox.SetValueNoSignal(Mathf.Clamp(width, 1, MaxWidth));
		mapHeightSpinBox.SetValueNoSignal(Mathf.Clamp(height, 1, MaxHeight));
	}

	/// <summary>
	/// 获取当前输入框中的地图宽度。
	/// </summary>
	public int GetMapWidth()
	{
		return (int)mapWidthSpinBox.Value;
	}

	/// <summary>
	/// 获取当前输入框中的地图高度。
	/// </summary>
	public int GetMapHeight()
	{
		return (int)mapHeightSpinBox.Value;
	}

	// 响应确认按钮点击并触发地图尺寸重设。
	private void onConfirmResizeButtonPressed()
	{
		int width = GetMapWidth();
		int height = GetMapHeight();

		Node? geographyManager = GetTree().GetFirstNodeInGroup("geography_manager");
		if (geographyManager != null)
		{
			geographyManager.Call("resize_map", width, height);
		}

		EmitSignal(SignalName.ResizeConfirmed, width, height);
	}
}
