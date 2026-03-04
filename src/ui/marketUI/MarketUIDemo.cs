using Godot;
using System;
using System.Linq;

/// <summary>
/// MarketUI 组件演示入口。
/// </summary>
[GlobalClass]
public partial class MarketUIDemo : Control
{
	// 市场面板实例。
	private MarketUI marketUi = null!;
	// 产品市场实例。
	private ProductMarket productMarket = null!;
	// 劳动力市场实例。
	private LabourMarket labourMarket = null!;

	/// <summary>
	/// 初始化 Demo。
	/// </summary>
	public override void _Ready()
	{
		marketUi = GetNode<MarketUI>("%MarketUI");
		Button randomizeButton = GetNode<Button>("ToolbarMargin/Toolbar/RandomizeButton");
		Button toggleButton = GetNode<Button>("ToolbarMargin/Toolbar/ToggleButton");

		productMarket = new ProductMarket();
		labourMarket = new LabourMarket();

		marketUi.Setup();
		marketUi.BindMarket(productMarket, labourMarket);
		marketUi.Visible = true;

		randomizeButton.Pressed += randomizeMarketData;
		toggleButton.Pressed += marketUi.Toggle;

		randomizeMarketData();
	}

	// 随机刷新市场数据并触发显示更新。
	private void randomizeMarketData()
	{
		Enum.GetValues<ProductType.Enums>()
			.ToList()
			.ForEach(type =>
			{
				float demand = 20.0f + (float)Random.Shared.NextDouble() * 180.0f;
				float supply = 20.0f + (float)Random.Shared.NextDouble() * 180.0f;
				float price = 10.0f + (float)Random.Shared.NextDouble() * 120.0f;

				productMarket.ConsumerDemands.Set(type, demand * 0.65f);
				productMarket.IndustryDemands.Set(type, demand * 0.35f);
				productMarket.Supplies.Set(type, supply);
				productMarket.Prices.Set(type, price);
			});

		Enum.GetValues<JobType.Enums>()
			.ToList()
			.ForEach(jobType =>
			{
				float wage = 8.0f + (float)Random.Shared.NextDouble() * 32.0f;
				float demand = 5.0f + (float)Random.Shared.NextDouble() * 60.0f;
				float supply = 5.0f + (float)Random.Shared.NextDouble() * 60.0f;
				labourMarket.JobPrices.Set(jobType, wage);
				labourMarket.JobDemands.Set(jobType, demand);
				labourMarket.JobSupplies.Set(jobType, supply);
			});

		productMarket.NotifyChanged();
		labourMarket.NotifyChanged();
	}
}
