using System.Linq;
using Godot;

/// <summary>
/// 信息内容渲染块，负责将 InfoData 渲染为可视化条目。
/// </summary>
[GlobalClass]
public partial class InfoContentBlock : ScrollContainer
{
	// 内容根容器。
	private VBoxContainer contentContainer = null!;

	/// <summary>
	/// 初始化内容节点。
	/// </summary>
	public override void _Ready()
	{
		contentContainer = GetNode<VBoxContainer>("%ContentContainer");
		HorizontalScrollMode = ScrollMode.Disabled;
	}

	/// <summary>
	/// 渲染 InfoData 数据。
	/// </summary>
	public void Render(InfoData infoData)
	{
		Clear();
		addStructuredItems(infoData);
	}

	/// <summary>
	/// 清空当前渲染内容。
	/// </summary>
	public void Clear()
	{
		contentContainer.GetChildren().ToList().ForEach(child => child.QueueFree());
	}

	/// <summary>
	/// 获取当前内容高度。
	/// </summary>
	public float GetContentHeight()
	{
		return contentContainer.GetCombinedMinimumSize().Y;
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

		contentContainer.AddChild(marginContainer);
		contentContainer.AddChild(new HSeparator());
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
		contentContainer.AddChild(rowContainer);
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
		contentContainer.AddChild(rowContainer);
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
