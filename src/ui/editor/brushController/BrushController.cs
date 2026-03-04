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

	// 绑定的地表编辑器状态对象。
	private GroundEditor groundEditor = null!;

	public override void _Ready()
	{
		brushSizeSpinBox = GetNode<SpinBox>("%BrushSizeSpinBox");
		brushSizeSpinBox.ValueChanged += onBrushSizeChanged;
		MouseFilter = MouseFilterEnum.Stop;
	}

	/// <summary>
	/// 绑定地表编辑器状态，实现笔刷大小双向同步。
	/// </summary>
	public void Setup(GroundEditor editor)
	{
		groundEditor = editor;
		groundEditor.BrushSizeChanged += onGroundEditorBrushSizeChanged;
		initBrushSize(groundEditor.BrushSize);
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
		groundEditor.SetBrushSize(brushSize); // 改变模型
	}

	// 响应模型状态变化。
	private void onGroundEditorBrushSizeChanged(int size)
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
