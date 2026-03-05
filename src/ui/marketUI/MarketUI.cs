using System;
using Godot;

/// <summary>
/// 产品市场面板控制器，负责展示产品供需与职业工资数据。
/// </summary>
[GlobalClass]
public partial class MarketUI : Control
{
	// 产品表格组件。
	private ProductMarketTableUI productTable = null!;
	// 职业表格组件。
	private LabourMarketTableUI jobTable = null!;
	// 当前绑定产品市场。
	private ProductMarket market = null!;
	// 当前绑定劳动力市场。
	private LabourMarket labourMarket = null!;
	// 是否已完成绑定。
	private bool isBound;

	/// <summary>
	/// 初始化节点引用。
	/// </summary>
	public override void _Ready()
	{
		productTable = GetNode<ProductMarketTableUI>("%ProductTable");
		jobTable = GetNode<LabourMarketTableUI>("%JobTable");
		Button closeButton = GetNode<Button>("PanelContainer/MarginContainer/VBoxContainer/CloseButton");
		closeButton.Pressed += Toggle;
	}

	/// <summary>
	/// 初始化面板状态。
	/// </summary>
	public void Setup()
	{
		isBound = false;
		Visible = false;
	}

	/// <summary>
	/// 绑定市场数据并刷新展示。
	/// </summary>
	public void BindMarket(ProductMarket market, LabourMarket labourMarket)
	{
		if (isBound)
		{
			this.market.MarketChanged -= UpdateDisplay;
			this.labourMarket.LabourMarketChanged -= UpdateDisplay;
		}

		this.market = market;
		this.labourMarket = labourMarket;

		this.market.MarketChanged += UpdateDisplay;
		this.labourMarket.LabourMarketChanged += UpdateDisplay;
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
	/// 刷新产品与职业数据展示。
	/// </summary>
	public void UpdateDisplay()
	{
		if (!isBound)
		{
			return;
		}

		productTable.RenderMarket(market);
		jobTable.RenderMarket(labourMarket);
	}
}
