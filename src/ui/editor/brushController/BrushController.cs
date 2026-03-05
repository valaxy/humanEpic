using Godot;

/// <summary>
/// 笔刷参数控制器。
/// 提供笔刷大小编辑与事件分发能力。
/// </summary>
[GlobalClass]
public partial class BrushController : HBoxContainer
{
	// 笔刷大小输入框。
	private SpinBox brushSizeSpinBox = null!;

	// 绑定的笔刷对象。
	private Brush brush = null!;

	public override void _Ready()
	{
		brushSizeSpinBox = GetNode<SpinBox>("%BrushSizeSpinBox");
		brushSizeSpinBox.ValueChanged += onBrushSizeChanged;
		MouseFilter = MouseFilterEnum.Stop;
	}

	/// <summary>
	/// 绑定笔刷对象，实现笔刷大小双向同步。
	/// </summary>
	public void Setup(Brush brush)
	{
		this.brush = brush;
		this.brush.SizeChanged -= onBrushSizeChangedFromModel;
		this.brush.SizeChanged += onBrushSizeChangedFromModel;
		initBrushSize(this.brush.Size);
	}


	// 设置笔刷大小数值。
	private void initBrushSize(int value)
	{
		if ((int)brushSizeSpinBox.Value != value)
		{
			brushSizeSpinBox.SetValueNoSignal(value);
		}
	}

	// 响应输入框数值变化。
	private void onBrushSizeChanged(double value)
	{
		int brushSize = (int)value;
		brush.SetSize(brushSize);
	}

	// 响应模型状态变化。
	private void onBrushSizeChangedFromModel(int size)
	{
		if ((int)brushSizeSpinBox.Value != size)
		{
			brushSizeSpinBox.SetValueNoSignal(size);
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButtonEvent)
		{
			if (mouseButtonEvent.ButtonIndex == MouseButton.WheelUp || mouseButtonEvent.ButtonIndex == MouseButton.WheelDown)
			{
				AcceptEvent();
			}
		}
	}
}
