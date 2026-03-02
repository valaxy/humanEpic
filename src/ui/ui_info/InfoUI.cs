using System.Linq;
using Godot;

/// <summary>
/// 通用信息展示 UI 节点，提供灵活的 KV 数据绑定能力。
/// </summary>
[GlobalClass]
public partial class InfoUI : CanvasLayer
{
	// 信息面板标题。
	private Label titleLabel = null!;

	// 用于放置 KV 信息条目的容器。
	private VBoxContainer kvContainer = null!;

	// 滚动容器。
	private ScrollContainer scrollContainer = null!;

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
		kvContainer = GetNode<VBoxContainer>("%KVContainer");
		scrollContainer = GetNode<ScrollContainer>("MainPanel/VBoxContainer/ScrollContainer");
		closeButton = GetNode<Button>("%CloseButton");
		mainPanel = GetNode<Control>("MainPanel");

		Visible = false;
		initializeScrollContainer();
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

		kvContainer.GetChildren().ToList().ForEach(child => child.QueueFree());
		addStructuredItems(data);

		Visible = true;
		updatePanelHeight();
	}


	// 动态计算并更新面板高度。
	private async void updatePanelHeight()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		float baseFixedHeight = 60.0f;
		float contentHeight = kvContainer.GetCombinedMinimumSize().Y;
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




	// 批量添加结构化 KV 项。
	private void addStructuredItems(InfoData infoData)
	{
		infoData.Entries.ToList().ForEach(entry => addStructuredItem(entry.key, entry.value));
	}

	// 添加单个结构化 KV 项。
	private void addStructuredItem(string key, InfoEntryData entryData)
	{
		if (entryData is InfoGroupEntryData groupEntry)
		{
			addCategoryHeader(key);
			addStructuredItems(groupEntry.Value);
			return;
		}

		if (entryData is InfoProgressEntryData progressEntry)
		{
			addProgressKvItem(key, progressEntry);
			return;
		}

		addTextKvItem(key, entryData.ToText());
	}

	// 添加一级分类头部和分割线。
	private void addCategoryHeader(string titleText)
	{
		Label header = new Label();
		header.Text = titleText;
		header.AddThemeFontSizeOverride("font_size", 14);
		header.AddThemeColorOverride("font_color", Colors.YellowGreen);

		MarginContainer marginContainer = new MarginContainer();
		marginContainer.AddThemeConstantOverride("margin_top", 10);
		marginContainer.AddChild(header);

		kvContainer.AddChild(marginContainer);
		kvContainer.AddChild(new HSeparator());
	}

	// 添加文本型 KV 显示项。
	private void addTextKvItem(string keyText, string valueText)
	{
		HBoxContainer rowContainer = new HBoxContainer();
		rowContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		Label keyLabel = new Label();
		keyLabel.Text = $"{keyText}: ";
		keyLabel.AddThemeColorOverride("font_color", Colors.Gray);
		rowContainer.AddChild(keyLabel);

		Label valueLabel = new Label();
		valueLabel.Text = valueText;
		valueLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		valueLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		rowContainer.AddChild(valueLabel);
		kvContainer.AddChild(rowContainer);
	}

	// 添加进度型 KV 显示项。
	private void addProgressKvItem(string keyText, InfoProgressEntryData progressEntry)
	{
		HBoxContainer rowContainer = new HBoxContainer();
		rowContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		Label keyLabel = new Label();
		keyLabel.Text = $"{keyText}: ";
		keyLabel.AddThemeColorOverride("font_color", Colors.Gray);
		rowContainer.AddChild(keyLabel);

		ProgressBar progressBar = createProgressBar(progressEntry.Progress, progressEntry.ValueText);
		progressBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		rowContainer.AddChild(progressBar);
		kvContainer.AddChild(rowContainer);
	}

	// 初始化滚动容器，禁用横向滚动。
	private void initializeScrollContainer()
	{
		scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
	}

	// 初始化关闭按钮样式与交互。
	private void initializeCloseButton()
	{
		closeButton.CustomMinimumSize = new Vector2(36.0f, 36.0f);
		closeButton.AddThemeFontSizeOverride("font_size", 22);
	}

	// 创建一个视觉进度条。
	private static ProgressBar createProgressBar(float ratio, string scoreText = "")
	{
		ProgressBar progressBar = new ProgressBar();
		progressBar.MinValue = 0;
		progressBar.MaxValue = 100;
		progressBar.Value = Mathf.Clamp(ratio, 0.0f, 1.0f) * 100.0f;
		progressBar.CustomMinimumSize = new Vector2(0.0f, 16.0f);
		progressBar.ShowPercentage = false;

		if (ratio > 1.0f)
		{
			StyleBoxFlat fillStyle = new StyleBoxFlat();
			fillStyle.BgColor = new Color(0.85f, 0.25f, 0.25f);
			progressBar.AddThemeStyleboxOverride("fill", fillStyle);
		}

		Label centerLabel = new Label();
		centerLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		centerLabel.Text = string.IsNullOrEmpty(scoreText) ? ratio.ToString("0.00") : scoreText;
		centerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		centerLabel.VerticalAlignment = VerticalAlignment.Center;
		centerLabel.AnchorRight = 1.0f;
		centerLabel.AnchorBottom = 1.0f;
		centerLabel.OffsetLeft = 0.0f;
		centerLabel.OffsetTop = 0.0f;
		centerLabel.OffsetRight = 0.0f;
		centerLabel.OffsetBottom = 0.0f;
		centerLabel.AddThemeFontSizeOverride("font_size", 12);
		progressBar.AddChild(centerLabel);

		return progressBar;
	}
}