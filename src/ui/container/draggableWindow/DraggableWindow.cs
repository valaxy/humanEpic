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
	// 拖拽时可活动区域。
	private Rect2 dragBounds;

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
		Panel panel = GetNode<Panel>("Panel");
		VBoxContainer rootVBox = GetNode<VBoxContainer>("VBoxContainer");
		dragBounds = getDragBounds();

		MouseFilter = MouseFilterEnum.Stop;
		panel.MouseFilter = MouseFilterEnum.Stop;
		rootVBox.MouseFilter = MouseFilterEnum.Stop;
		titleBar.MouseFilter = MouseFilterEnum.Stop;
		contentRoot.MouseFilter = MouseFilterEnum.Stop;

		titleBar.GuiInput += onTitleBarGuiInput;
		GuiInput += onWindowGuiInput;
		closeButton.Pressed += () => EmitSignal(SignalName.CloseRequested);
		closeButton.Text = string.Empty;
		closeButton.Icon = ThemeDB.GetDefaultTheme().GetIcon("Close", "EditorIcons");
		closeButton.Flat = true;
	}

	/// <summary>
	/// 同步窗口尺寸变化时的拖拽边界。
	/// </summary>
	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			dragBounds = getDragBounds();
			Position = clampPosition(Position);
		}
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
			dragBounds = getDragBounds();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (isDragging && inputEvent is InputEventMouseMotion motion)
		{
			Position = clampPosition(Position + motion.Relative);
			GetViewport().SetInputAsHandled();
		}
	}

	// 拦截窗口上的鼠标事件，避免传递到游戏世界。
	private void onWindowGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton || inputEvent is InputEventMouseMotion)
		{
			GetViewport().SetInputAsHandled();
		}
	}

	// 计算拖拽可活动区域。
	private Rect2 getDragBounds()
	{
		if (GetParent() is Control parentControl)
		{
			return new Rect2(Vector2.Zero, parentControl.Size);
		}

		return new Rect2(Vector2.Zero, GetViewportRect().Size);
	}

	// 将窗口位置钳制在可活动区域内。
	private Vector2 clampPosition(Vector2 targetPosition)
	{
		float maxX = dragBounds.Position.X + Mathf.Max(0.0f, dragBounds.Size.X - Size.X);
		float maxY = dragBounds.Position.Y + Mathf.Max(0.0f, dragBounds.Size.Y - Size.Y);
		float clampedX = Mathf.Clamp(targetPosition.X, dragBounds.Position.X, maxX);
		float clampedY = Mathf.Clamp(targetPosition.Y, dragBounds.Position.Y, maxY);
		return new Vector2(clampedX, clampedY);
	}
}
