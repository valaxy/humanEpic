using Godot;

/// <summary>
/// 通用信息面板容器，负责标题栏、关闭与布局。
/// </summary>
[GlobalClass]
public partial class InfoUI : CanvasLayer
{
	// 信息面板标题。
	private Label titleLabel = null!;
	// 内容渲染块。
	private InfoContentBlock contentBlock = null!;

	// 关闭按钮。
	private Button closeButton = null!;

	// 面板主背景，用于拦截输入。
	private Control mainPanel = null!;

	/// <summary>
	/// 初始化组件引用和交互绑定。
	/// </summary>
	public override void _Ready()
	{
		titleLabel = GetNode<Label>("%TitleLabel");
		contentBlock = GetNode<InfoContentBlock>("%ContentBlock");
		closeButton = GetNode<Button>("%CloseButton");
		mainPanel = GetNode<Control>("MainPanel");

		Visible = false;
		initializeCloseButton();
		closeButton.Pressed += onCloseButtonPressed;
		mainPanel.GuiInput += onMainPanelGuiInput;
	}

	/// <summary>
	/// 设置面板的水平偏移位置。
	/// </summary>
	/// <param name="xOffset">水平偏移。</param>
	public void SetPositionOffset(float xOffset)
	{
		mainPanel.OffsetLeft = 20.0f + xOffset;
		mainPanel.OffsetRight = 320.0f + xOffset;
	}

	/// <summary>
	/// 根据传递数据展示详细信息。
	/// </summary>
	/// <param name="title">面板标题。</param>
	/// <param name="data">展示数据，使用强类型结构化信息字典。</param>
	public void ShowInfo(string title, InfoData data)
	{
		titleLabel.Text = title;
		contentBlock.Render(data);

		Visible = true;
		updatePanelHeight();
	}


	// 动态计算并更新面板高度。
	private async void updatePanelHeight()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		float baseFixedHeight = 60.0f;
		float contentHeight = contentBlock.GetContentHeight();
		float totalTargetHeight = Mathf.Clamp(baseFixedHeight + contentHeight + 40.0f, 200.0f, 600.0f);
		mainPanel.OffsetTop = mainPanel.OffsetBottom - totalTargetHeight;
	}

	/// <summary>
	/// 隐藏当前信息面板。
	/// </summary>
	/// <param name="title">可选标题，仅当标题匹配时执行隐藏。</param>
	public void HideInfo(string title = "")
	{
		if (!string.IsNullOrEmpty(title) && titleLabel.Text != title)
		{
			return;
		}

		contentBlock.Clear();
		Visible = false;
	}

	// 关闭按钮点击处理。
	private void onCloseButtonPressed()
	{
		HideInfo();
	}

	// 阻止界面交互穿透到底层场景。
	private void onMainPanelGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton)
		{
			GetViewport().SetInputAsHandled();
		}
	}

	// 初始化关闭按钮样式与交互。
	private void initializeCloseButton()
	{
		closeButton.CustomMinimumSize = new Vector2(36.0f, 36.0f);
		closeButton.AddThemeFontSizeOverride("font_size", 22);
	}
}
