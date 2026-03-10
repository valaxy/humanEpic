using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 建筑信息总面板，使用模块格子承载建筑各功能信息。
/// </summary>
[GlobalClass]
public partial class BuildingInfoPanelUI : CanvasLayer
{
	// 产品市场表场景。
	private static readonly PackedScene productMarketTableScene = GD.Load<PackedScene>("res://src/ui/marketUI/marketTable/product_market_table_ui.tscn");
	// 产品市场历史图场景。
	private static readonly PackedScene productMarketHistoryChartScene = GD.Load<PackedScene>("res://src/ui/marketUI/priceHistory/product_market_history_chart_ui.tscn");

	// 劳动力市场表场景。
	private static readonly PackedScene labourMarketTableScene = GD.Load<PackedScene>("res://src/ui/marketUI/marketTable/labour_market_table_ui.tscn");

	// 通用侧边栏容器。
	private SidePanel sidePanel = null!;

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

	// 建筑集合。
	private BuildingCollection buildingCollection = null!;

	// 当前展示建筑。
	private Building currentBuilding = null!;
	// 当前绑定商品市场。
	private ProductMarket activeProductMarket = null!;
	// 当前绑定劳动力市场。
	private LabourMarket activeLabourMarket = null!;
	// 当前是否已绑定市场刷新事件。
	private bool isMarketBound;

	// 商品市场表。
	private ProductMarketTableUI productTable = null!;
	// 商品折线图容器。
	private VBoxContainer productChartContainer = null!;
	// 商品折线图标题。
	private Label productChartTitle = null!;
	// 商品折线图关闭按钮。
	private Button productChartCloseButton = null!;
	// 商品折线图组件。
	private ProductMarketHistoryChartUI productHistoryChart = null!;
	// 当前选中的商品。
	private ProductType.Enums? selectedProductType;

	// 劳动力市场表。
	private LabourMarketTableUI labourTable = null!;

	/// <summary>
	/// 初始化节点引用。
	/// </summary>
	public override void _Ready()
	{
		sidePanel = GetNode<SidePanel>("%SidePanel");
		infoContent = GetNode<VBoxContainer>("%InfoContent");
		productMarketModuleCard = GetNode<PanelContainer>("%ProductMarketModuleCard");
		productMarketSlot = GetNode<VBoxContainer>("%ProductMarketSlot");
		labourMarketModuleCard = GetNode<PanelContainer>("%LabourMarketModuleCard");
		labourMarketSlot = GetNode<VBoxContainer>("%LabourMarketSlot");

		Visible = false;
		sidePanel.CloseRequested += hidePanel;
		setMarketModulesVisible(false);
	}

	/// <summary>
	/// 退出树前清理事件订阅。
	/// </summary>
	public override void _ExitTree()
	{
		unbindMarketEvents();
		if (productTable != null)
		{
			productTable.ProductRowPressed -= onProductRowPressed;
		}
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
		Building building = buildingCollection.GetByPos(cellPos);
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
		currentBuilding = building;
		sidePanel.SetTitle($"建筑信息 - {building.Name}");
		clearInfoEntries();
		appendInfoEntries(infoContent, building.GetInfoData());
		clearMarketSlots();
		unbindMarketEvents();
		selectedProductType = null;

		if (building.Market != null)
		{
			productTable = productMarketTableScene.Instantiate<ProductMarketTableUI>();
			productMarketSlot.AddChild(productTable); // 必须先addChild后Render
			productTable.ProductRowPressed += onProductRowPressed;

			productChartContainer = createProductChartContainer();
			productHistoryChart = productMarketHistoryChartScene.Instantiate<ProductMarketHistoryChartUI>();
			productChartContainer.AddChild(productHistoryChart);
			productMarketSlot.AddChild(productChartContainer);
			productChartContainer.Visible = false;

			activeProductMarket = building.Market.ProductMarket;
			activeLabourMarket = building.Market.LabourMarket;
			isMarketBound = true;
			activeProductMarket.Changed += onProductMarketChanged;
			activeLabourMarket.Changed += onLabourMarketChanged;

			refreshProductMarketSection();

			labourTable = labourMarketTableScene.Instantiate<LabourMarketTableUI>();
			labourMarketSlot.AddChild(labourTable); // 必须先addChild后Render
			labourTable.RenderMarket(activeLabourMarket);
			setMarketModulesVisible(true);
		}
		else
		{
			setMarketModulesVisible(false);
		}

		Visible = true;
	}

	// 商品市场数据更新时刷新。
	private void onProductMarketChanged()
	{
		refreshProductMarketSection();
	}

	// 劳动力市场数据更新时刷新。
	private void onLabourMarketChanged()
	{
		if (labourTable != null)
		{
			labourTable.RenderMarket(activeLabourMarket);
		}
	}

	// 点击商品行。
	private void onProductRowPressed(ProductType.Enums productType)
	{
		selectedProductType = selectedProductType.HasValue && selectedProductType.Value == productType
			? null
			: productType;
		refreshProductMarketSection();
	}

	// 关闭商品折线图。
	private void onProductChartClosePressed()
	{
		selectedProductType = null;
		refreshProductMarketSection();
	}

	// 刷新商品市场区块（表格 + 折线图显隐）。
	private void refreshProductMarketSection()
	{
		if (productTable != null)
		{
			productTable.RenderMarket(activeProductMarket, selectedProductType);
		}

		if (productChartContainer == null)
		{
			return;
		}

		if (!selectedProductType.HasValue)
		{
			productChartContainer.Visible = false;
			return;
		}

		productChartContainer.Visible = true;
		ProductType.Enums selectedType = selectedProductType.Value;
		string productName = ProductTemplate.GetTemplate(selectedType).Name;
		productChartTitle.Text = $"{productName} 价格历史";
		productHistoryChart.RenderSingleProduct(activeProductMarket, selectedType);
	}

	// 创建商品折线图容器。
	private VBoxContainer createProductChartContainer()
	{
		VBoxContainer container = new VBoxContainer();
		container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		container.AddThemeConstantOverride("separation", 6);

		HBoxContainer headerRow = new HBoxContainer();
		headerRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		productChartTitle = new Label();
		productChartTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		productChartTitle.Text = "商品价格历史";
		productChartTitle.HorizontalAlignment = HorizontalAlignment.Left;

		productChartCloseButton = new Button();
		productChartCloseButton.Text = "关闭";
		productChartCloseButton.Flat = true;
		productChartCloseButton.Pressed += onProductChartClosePressed;

		headerRow.AddChild(productChartTitle);
		headerRow.AddChild(productChartCloseButton);
		container.AddChild(headerRow);

		HSeparator separator = new HSeparator();
		container.AddChild(separator);

		return container;
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
		if (productTable != null)
		{
			productTable.ProductRowPressed -= onProductRowPressed;
		}

		if (productChartCloseButton != null)
		{
			productChartCloseButton.Pressed -= onProductChartClosePressed;
		}

		List<Node> productChildren = productMarketSlot.GetChildren().Cast<Node>().ToList();
		productChildren.ForEach(child => child.QueueFree());

		List<Node> labourChildren = labourMarketSlot.GetChildren().Cast<Node>().ToList();
		labourChildren.ForEach(child => child.QueueFree());

		productTable = null!;
		productChartContainer = null!;
		productChartTitle = null!;
		productChartCloseButton = null!;
		productHistoryChart = null!;
		labourTable = null!;
	}

	// 解除市场事件绑定。
	private void unbindMarketEvents()
	{
		if (!isMarketBound)
		{
			return;
		}

		activeProductMarket.Changed -= onProductMarketChanged;
		activeLabourMarket.Changed -= onLabourMarketChanged;
		isMarketBound = false;
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
		unbindMarketEvents();
		selectedProductType = null;
	}
}
