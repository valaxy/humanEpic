using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 建筑信息总面板，使用模块格子承载建筑各功能信息。
/// </summary>
[GlobalClass]
public partial class BuildingInfoPanelUI : CanvasLayer
{
	// 产品市场表场景。
	private static readonly PackedScene productMarketTableScene = GD.Load<PackedScene>("res://src/ui/marketUI/components/product_market_table_ui.tscn");

	// 劳动力市场表场景。
	private static readonly PackedScene labourMarketTableScene = GD.Load<PackedScene>("res://src/ui/marketUI/components/labour_market_table_ui.tscn");

	// 面板主节点。
	private Control mainPanel = null!;

	// 标题节点。
	private Label titleLabel = null!;

	// 模块格子容器。
	private GridContainer moduleGrid = null!;

	// 关闭按钮。
	private Button closeButton = null!;

	// 建筑集合。
	private BuildingCollection buildingCollection = null!;

	/// <summary>
	/// 初始化节点引用。
	/// </summary>
	public override void _Ready()
	{
		mainPanel = GetNode<Control>("MainPanel");
		titleLabel = GetNode<Label>("MainPanel/VBoxContainer/TitleLabel");
		moduleGrid = GetNode<GridContainer>("MainPanel/VBoxContainer/ScrollContainer/ModuleGrid");
		closeButton = GetNode<Button>("MainPanel/CloseButton");

		Visible = false;
		closeButton.Pressed += hidePanel;
	}

	/// <summary>
	/// 绑定选择系统。
	/// </summary>
	public void Setup(BuildingCollection buildingCollection, GroundSelection selection)
	{
		this.buildingCollection = buildingCollection;
		selection.BuildingSelected += onBuildingSelected;
		selection.BuildingSelectionCleared += onBuildingSelectionCleared;
	}

	// 响应建筑选中。
	private void onBuildingSelected(Vector2I cellPos)
	{
		Building building = buildingCollection.Get(cellPos);
		renderBuildingModules(building);
	}

	// 响应建筑选中清理。
	private void onBuildingSelectionCleared()
	{
		hidePanel();
	}

	// 渲染建筑模块信息。
	private void renderBuildingModules(Building building)
	{
		titleLabel.Text = $"建筑信息 - {building.Name}";
		clearModules();
		moduleGrid.AddChild(createInfoModuleCard("Info", building.GetInfoData()));

		if (building.Market != null)
		{
			ProductMarketTableUI productTable = productMarketTableScene.Instantiate<ProductMarketTableUI>();
			productTable.RenderMarket(building.Market.ProductMarket);
			moduleGrid.AddChild(createControlModuleCard("产品市场", productTable));

			LabourMarketTableUI labourTable = labourMarketTableScene.Instantiate<LabourMarketTableUI>();
			labourTable.RenderMarket(building.Market.LabourMarket);
			moduleGrid.AddChild(createControlModuleCard("劳动力市场", labourTable));
		}

		Visible = true;
		updatePanelHeight();
	}

	// 创建 InfoData 模块卡片。
	private Control createInfoModuleCard(string title, InfoData data)
	{
		VBoxContainer content = new VBoxContainer();
		content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		appendInfoEntries(content, data);
		return createControlModuleCard(title, content);
	}

	// 创建通用控件模块卡片。
	private Control createControlModuleCard(string title, Control content)
	{
		PanelContainer panel = new PanelContainer();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

		MarginContainer margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);

		VBoxContainer wrapper = new VBoxContainer();
		wrapper.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		wrapper.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		wrapper.AddThemeConstantOverride("separation", 8);

		Label moduleTitle = new Label();
		moduleTitle.Text = title;
		moduleTitle.AddThemeFontSizeOverride("font_size", 16);
		moduleTitle.AddThemeColorOverride("font_color", Colors.YellowGreen);

		wrapper.AddChild(moduleTitle);
		wrapper.AddChild(new HSeparator());
		wrapper.AddChild(content);
		margin.AddChild(wrapper);
		panel.AddChild(margin);
		return panel;
	}

	// 将结构化信息追加为 KV 行。
	private void appendInfoEntries(VBoxContainer container, InfoData infoData)
	{
		infoData.Entries
			.ToList()
			.ForEach(entry => appendInfoEntry(container, entry.key, entry.value));
	}

	// 追加单个结构化信息条目。
	private void appendInfoEntry(VBoxContainer container, string key, InfoEntryData entryData)
	{
		if (entryData is InfoGroupEntryData groupEntry)
		{
			Label header = new Label();
			header.Text = key;
			header.AddThemeColorOverride("font_color", Colors.LightSteelBlue);
			container.AddChild(header);
			appendInfoEntries(container, groupEntry.Value);
			return;
		}

		if (entryData is InfoProgressEntryData progressEntry)
		{
			container.AddChild(createProgressRow(key, progressEntry));
			return;
		}

		container.AddChild(createTextRow(key, entryData.ToText()));
	}

	// 创建文本 KV 行。
	private static Control createTextRow(string key, string value)
	{
		HBoxContainer row = new HBoxContainer();
		Label keyLabel = new Label();
		keyLabel.Text = $"{key}: ";
		keyLabel.AddThemeColorOverride("font_color", Colors.Gray);

		Label valueLabel = new Label();
		valueLabel.Text = value;
		valueLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		valueLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		row.AddChild(keyLabel);
		row.AddChild(valueLabel);
		return row;
	}

	// 创建进度 KV 行。
	private static Control createProgressRow(string key, InfoProgressEntryData progressEntry)
	{
		HBoxContainer row = new HBoxContainer();
		Label keyLabel = new Label();
		keyLabel.Text = $"{key}: ";
		keyLabel.AddThemeColorOverride("font_color", Colors.Gray);

		ProgressBar progressBar = new ProgressBar();
		progressBar.MinValue = 0;
		progressBar.MaxValue = 100;
		progressBar.Value = Mathf.Clamp(progressEntry.Progress, 0.0f, 1.0f) * 100.0f;
		progressBar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		progressBar.ShowPercentage = false;

		Label progressLabel = new Label();
		progressLabel.Text = string.IsNullOrEmpty(progressEntry.ValueText)
			? progressEntry.Progress.ToString("0.00")
			: progressEntry.ValueText;
		progressLabel.HorizontalAlignment = HorizontalAlignment.Center;
		progressLabel.VerticalAlignment = VerticalAlignment.Center;
		progressLabel.AnchorRight = 1.0f;
		progressLabel.AnchorBottom = 1.0f;
		progressLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		progressBar.AddChild(progressLabel);

		row.AddChild(keyLabel);
		row.AddChild(progressBar);
		return row;
	}

	// 清理已有模块。
	private void clearModules()
	{
		List<Node> children = moduleGrid.GetChildren().Cast<Node>().ToList();
		children.ForEach(child => child.QueueFree());
	}

	// 隐藏面板。
	private void hidePanel()
	{
		Visible = false;
	}

	// 动态调整面板高度以适配内容。
	private async void updatePanelHeight()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		float baseFixedHeight = 84.0f;
		float contentHeight = moduleGrid.GetCombinedMinimumSize().Y;
		float totalTargetHeight = Mathf.Clamp(baseFixedHeight + contentHeight + 30.0f, 240.0f, 760.0f);
		mainPanel.OffsetTop = mainPanel.OffsetBottom - totalTargetHeight;
	}
}
