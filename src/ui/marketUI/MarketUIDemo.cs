using System;
using System.Linq;
using Godot;

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
	// 演示中递增代理编号。
	private int nextAgentId = 1;

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
				float price = 20.0f + (float)Random.Shared.NextDouble() * 180.0f;
				productMarket.SetPrice(type, price);

				Enumerable.Range(0, 4)
					.ToList()
					.ForEach(index =>
					{
						float buyPrice = price * (0.6f + 0.3f * (float)Random.Shared.NextDouble());
						float sellPrice = price * (1.0f + 0.4f * (float)Random.Shared.NextDouble());
						int quantity = Random.Shared.Next(20, 120);
						productMarket.PlaceBuyOrder(type, buyPrice, quantity, nextAgentId++);
						productMarket.PlaceSellOrder(type, sellPrice, quantity, nextAgentId++);
					});
			});

		Enum.GetValues<JobType.Enums>()
			.ToList()
			.ForEach(jobType =>
			{
				float wage = 10.0f + (float)Random.Shared.NextDouble() * 35.0f;
				labourMarket.SetPrice(jobType, wage);

				labourMarket.PlaceBuyOrder(jobType, wage * 0.95f, Random.Shared.Next(10, 60), nextAgentId++);
				labourMarket.PlaceSellOrder(jobType, wage * 1.05f, Random.Shared.Next(10, 60), nextAgentId++);
			});

		string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		productMarket.RecordPriceSnapshot(dt);
		labourMarket.RecordPriceSnapshot(dt);
		productMarket.NotifyChanged();
		labourMarket.NotifyChanged();
	}
}
