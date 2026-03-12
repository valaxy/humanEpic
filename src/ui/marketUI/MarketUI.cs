using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 市场面板控制器，负责展示商品/劳动力总览、挂单簿与价格历史。
/// </summary>
[GlobalClass]
public partial class MarketUI : Control
{
	// 商品市场表。
	private ProductMarketTableUI productTable = null!;
	// 劳动力市场表。
	private LabourMarketTableUI labourTable = null!;
	// 买单表。
	private DataTableView buyOrderTable = null!;
	// 卖单表。
	private DataTableView sellOrderTable = null!;
	// 商品历史折线图。
	private ProductMarketHistoryChartUI productHistoryChart = null!;
	// 商品历史标题。
	private Label historyTitleLabel = null!;
	// 当前商品市场。
	private ProductMarket productMarket = null!;
	// 当前劳动力市场。
	private LabourMarket labourMarket = null!;
	// 当前选中的商品。
	private ProductType.Enums? selectedProduct;
	// 当前是否已完成绑定。
	private bool isBound;

	/// <summary>
	/// 初始化节点引用。
	/// </summary>
	public override void _Ready()
	{
		productTable = GetNode<ProductMarketTableUI>("%ProductTable");
		labourTable = GetNode<LabourMarketTableUI>("%LabourTable");
		buyOrderTable = GetNode<DataTableView>("%BuyOrderTable");
		sellOrderTable = GetNode<DataTableView>("%SellOrderTable");
		productHistoryChart = GetNode<ProductMarketHistoryChartUI>("%ProductHistoryChart");
		historyTitleLabel = GetNode<Label>("%HistoryTitleLabel");

		List<Button> closeButtons = GetTree()
			.GetNodesInGroup("market_ui_close_button")
			.Cast<Button>()
			.ToList();
		closeButtons.ForEach(button => button.Pressed += Toggle);

		productTable.ProductRowPressed += onProductRowPressed;
	}

	/// <summary>
	/// 初始化面板状态。
	/// </summary>
	public void Setup()
	{
		Visible = true;
		isBound = false;
		selectedProduct = null;
	}

	/// <summary>
	/// 绑定市场数据并刷新展示。
	/// </summary>
	public void BindMarket(ProductMarket productMarket, LabourMarket labourMarket)
	{
		if (isBound)
		{
			this.productMarket.Changed -= UpdateDisplay;
			this.labourMarket.Changed -= UpdateDisplay;
		}

		this.productMarket = productMarket;
		this.labourMarket = labourMarket;
		selectedProduct = Enum.GetValues<ProductType.Enums>().FirstOrDefault();

		this.productMarket.Changed += UpdateDisplay;
		this.labourMarket.Changed += UpdateDisplay;
		isBound = true;
		UpdateDisplay();
	}

	/// <summary>
	/// 切换面板显示状态。
	/// </summary>
	public void Toggle()
	{
		Visible = !Visible;
	}

	/// <summary>
	/// 刷新全部面板内容。
	/// </summary>
	public void UpdateDisplay()
	{
		if (!isBound)
		{
			return;
		}

		productTable.RenderMarket(productMarket, selectedProduct);
		labourTable.RenderMarket(labourMarket);
		renderOrderBooks();
		renderHistory();
	}

	// 处理商品行点击。
	private void onProductRowPressed(ProductType.Enums productType)
	{
		selectedProduct = selectedProduct.HasValue && selectedProduct.Value == productType
			? null
			: productType;
		UpdateDisplay();
	}

	// 渲染买卖盘。
	private void renderOrderBooks()
	{
		if (!selectedProduct.HasValue)
		{
			renderOrderTable(buyOrderTable, "买单", []);
			renderOrderTable(sellOrderTable, "卖单", []);
			return;
		}

		ProductType.Enums productType = selectedProduct.Value;
		IReadOnlyList<(float price, int quantity)> buyOrders = productMarket.GetBuyOrders(productType);
		IReadOnlyList<(float price, int quantity)> sellOrders = productMarket.GetSellOrders(productType);
		renderOrderTable(buyOrderTable, "买单", buyOrders);
		renderOrderTable(sellOrderTable, "卖单", sellOrders);
	}

	// 渲染单边订单簿表格。
	private static void renderOrderTable(DataTableView table, string title, IReadOnlyList<(float price, int quantity)> orders)
	{
		List<List<string>> rows = orders
			.Select(order => new List<string>
			{
				$"{order.price:0.##}",
				order.quantity.ToString()
			})
			.ToList();

		DataSource source = DataTableDataSourceFactory.Create(title, ["价格", "数量"], rows);
		DataTable config = DataTable.Create(title, [DataTextAlignment.Right, DataTextAlignment.Right], [DataTextAlignment.Right, DataTextAlignment.Right], sortableColumns: [0, 1]);
		table.Render(source, config);
	}

	// 渲染商品历史。
	private void renderHistory()
	{
		if (!selectedProduct.HasValue)
		{
			historyTitleLabel.Text = "点击左侧商品查看价格历史";
			productHistoryChart.Render(LineChartDataSourceFactory.Create("商品价格历史", [], []));
			return;
		}

		ProductType.Enums productType = selectedProduct.Value;
		historyTitleLabel.Text = $"{ProductTemplate.GetTemplate(productType).Name} 折线图";
		productHistoryChart.RenderSingleProduct(productMarket, productType);
	}
}
