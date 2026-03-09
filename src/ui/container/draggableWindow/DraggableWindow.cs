using Godot;

/// <summary>
/// 可拖拽窗口容器，支持标题栏拖动与关闭。
/// </summary>
[GlobalClass]
public partial class DraggableWindow : Control
{
	/// <summary>
	/// 点击关闭按钮时发出。
	/// </summary>
	[Signal]
	public delegate void CloseRequestedEventHandler();

	// 标题标签。
	private Label titleLabel = null!;
	// 标题栏拖拽热区。
	private Control titleBar = null!;
	// 关闭按钮。
	private Button closeButton = null!;
	// 内容插槽。
	private VBoxContainer contentRoot = null!;
	// 是否正在拖拽。
	private bool isDragging;

	/// <summary>
	/// 内容插槽根节点。
	/// </summary>
	public VBoxContainer ContentRoot => contentRoot;

	/// <summary>
	/// 初始化窗口交互。
	/// </summary>
	public override void _Ready()
	{
		titleLabel = GetNode<Label>("%TitleLabel");
		titleBar = GetNode<Control>("%TitleBar");
		closeButton = GetNode<Button>("%CloseButton");
		contentRoot = GetNode<VBoxContainer>("%ContentRoot");

		titleBar.GuiInput += onTitleBarGuiInput;
		closeButton.Pressed += () => EmitSignal(SignalName.CloseRequested);
	}

	/// <summary>
	/// 设置窗口标题。
	/// </summary>
	public void SetTitle(string title)
	{
		titleLabel.Text = title;
	}

	// 处理标题栏拖拽与拖拽结束。
	private void onTitleBarGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
		{
			isDragging = mouseButton.Pressed;
			GetViewport().SetInputAsHandled();
			return;
		}

		if (isDragging && inputEvent is InputEventMouseMotion motion)
		{
			Position += motion.Relative;
			GetViewport().SetInputAsHandled();
		}
	}
}
