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

	// 标题节点。
	private Label titleLabel = null!;

	// Info 模块内容容器。
	private VBoxContainer infoContent = null!;
	// 产品市场模块卡片。
	private PanelContainer productMarketModuleCard = null!;
	// 产品市场模块内容槽位。
	private VBoxContainer productMarketSlot = null!;
	// 劳动力市场模块卡片。
	private PanelContainer labourMarketModuleCard = null!;
	// 劳动力市场模块内容槽位。
	private VBoxContainer labourMarketSlot = null!;

	// 关闭按钮。
	private Button closeButton = null!;

	// 建筑集合。
	private BuildingCollection buildingCollection = null!;

	/// <summary>
	/// 初始化节点引用。
	/// </summary>
	public override void _Ready()
	{
		titleLabel = GetNode<Label>("MainPanel/VBoxContainer/TitleLabel");
		infoContent = GetNode<VBoxContainer>("MainPanel/VBoxContainer/ScrollContainer/ModuleGrid/InfoModuleCard/Margin/Wrapper/InfoContent");
		productMarketModuleCard = GetNode<PanelContainer>("MainPanel/VBoxContainer/ScrollContainer/ModuleGrid/ProductMarketModuleCard");
		productMarketSlot = GetNode<VBoxContainer>("MainPanel/VBoxContainer/ScrollContainer/ModuleGrid/ProductMarketModuleCard/Margin/Wrapper/ProductMarketSlot");
		labourMarketModuleCard = GetNode<PanelContainer>("MainPanel/VBoxContainer/ScrollContainer/ModuleGrid/LabourMarketModuleCard");
		labourMarketSlot = GetNode<VBoxContainer>("MainPanel/VBoxContainer/ScrollContainer/ModuleGrid/LabourMarketModuleCard/Margin/Wrapper/LabourMarketSlot");
		closeButton = GetNode<Button>("MainPanel/CloseButton");

		Visible = false;
		closeButton.Pressed += hidePanel;
		setMarketModulesVisible(false);
	}

	/// <summary>
	/// 绑定选择系统。
	/// </summary>
	public void Setup(BuildingCollection buildingCollection, GroundView groundView)
	{
		this.buildingCollection = buildingCollection;
		groundView.BuildingSelected += onBuildingSelected;
		groundView.BuildingSelectionCleared += onBuildingSelectionCleared;
	}

	/// <summary>
	/// 直接展示指定建筑信息（用于 Demo 或调试）。
	/// </summary>
	public void RenderBuilding(Building building)
	{
		renderBuildingModules(building);
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
		clearInfoEntries();
		appendInfoEntries(infoContent, building.GetInfoData());
		clearMarketSlots();

		if (building.Market != null)
		{
			ProductMarketTableUI productTable = productMarketTableScene.Instantiate<ProductMarketTableUI>();
			productMarketSlot.AddChild(productTable); // 必须先addChild后Render
			productTable.RenderMarket(building.Market.ProductMarket);

			LabourMarketTableUI labourTable = labourMarketTableScene.Instantiate<LabourMarketTableUI>();
			labourMarketSlot.AddChild(labourTable); // 必须先addChild后Render
			labourTable.RenderMarket(building.Market.LabourMarket);
			setMarketModulesVisible(true);
		}
		else
		{
			setMarketModulesVisible(false);
		}

		Visible = true;
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

	// 清理 Info 模块内容。
	private void clearInfoEntries()
	{
		List<Node> children = infoContent.GetChildren().Cast<Node>().ToList();
		children.ForEach(child => child.QueueFree());
	}

	// 清理市场模块内容。
	private void clearMarketSlots()
	{
		List<Node> productChildren = productMarketSlot.GetChildren().Cast<Node>().ToList();
		productChildren.ForEach(child => child.QueueFree());

		List<Node> labourChildren = labourMarketSlot.GetChildren().Cast<Node>().ToList();
		labourChildren.ForEach(child => child.QueueFree());
	}

	// 控制市场模块显隐。
	private void setMarketModulesVisible(bool visible)
	{
		productMarketModuleCard.Visible = visible;
		labourMarketModuleCard.Visible = visible;
	}

	// 隐藏面板。
	private void hidePanel()
	{
		Visible = false;
	}
}
