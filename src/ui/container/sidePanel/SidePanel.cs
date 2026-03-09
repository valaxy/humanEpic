using Godot;

/// <summary>
/// 通用侧边栏容器，提供标题、关闭按钮和可滚动内容区。
/// </summary>
[GlobalClass]
public partial class SidePanel : Control
{
	/// <summary>
	/// 点击关闭按钮时发出。
	/// </summary>
	[Signal]
	public delegate void CloseRequestedEventHandler();

	// 标题文本节点。
	private Label titleLabel = null!;
	// 可滚动容器。
	private ScrollContainer scrollContainer = null!;
	// 内容插槽根节点。
	private VBoxContainer contentRoot = null!;
	// 关闭按钮。
	private Button closeButton = null!;
	// 主面板。
	private Control mainPanel = null!;

	/// <summary>
	/// 内容插槽根节点，外部可向其中追加内容。
	/// </summary>
	public VBoxContainer ContentRoot => contentRoot;

	/// <summary>
	/// 初始化节点与交互。
	/// </summary>
	public override void _Ready()
	{
		titleLabel = GetNode<Label>("%TitleLabel");
		scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
		contentRoot = GetNode<VBoxContainer>("%ContentRoot");
		closeButton = GetNode<Button>("%CloseButton");
		mainPanel = GetNode<Control>("MainPanel");

		scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		closeButton.Pressed += () => EmitSignal(SignalName.CloseRequested);
		mainPanel.GuiInput += onMainPanelGuiInput;
	}

	/// <summary>
	/// 设置侧边栏标题。
	/// </summary>
	public void SetTitle(string title)
	{
		titleLabel.Text = title;
	}

	// 拦截滚轮输入，防止穿透到底层地面或镜头控制。
	private void onMainPanelGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseButton
			&& (mouseButton.ButtonIndex == MouseButton.WheelUp || mouseButton.ButtonIndex == MouseButton.WheelDown))
		{
			GetViewport().SetInputAsHandled();
		}
	}
}
