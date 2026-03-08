using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 市场系统，统一管理每种产品的需求量、产出量和价格。
/// </summary>
public class ProductMarket : Market<ProductType.Enums>
{
	private const string initialDt = "INIT";

	/// <summary>
	/// 市场数据变更事件。
	/// </summary>
	public event Action? MarketChanged;

	// 每种产品的消费需求量。
	public MarketDataBucket<ProductType.Enums> ConsumerDemands { get; }

	// 每种产品的工业加工需求量。
	public MarketDataBucket<ProductType.Enums> IndustryDemands { get; }

	// 每种产品的供应量。
	public MarketDataBucket<ProductType.Enums> Supplies { get; }

	// 每种产品的价格。
	public MarketDataBucket<ProductType.Enums> Prices { get; }

	// 商品价格历史。
	public PriceHistory<ProductType.Enums> PriceHistory { get; }

	// 商品价格下限，防止出现除零或极端价格。
	public const float MinProductPrice = 0.0001f; // TODO 在往上提一层？


	public ProductMarket()
		: base(ProductTemplate.GetTemplates().Keys)
	{
		IEnumerable<ProductType.Enums> productTypes = ProductTemplate.GetTemplates().Keys;
		ConsumerDemands = new MarketDataBucket<ProductType.Enums>(productTypes, _ => 0.0f);
		IndustryDemands = new MarketDataBucket<ProductType.Enums>(productTypes, _ => 0.0f);
		Supplies = new MarketDataBucket<ProductType.Enums>(productTypes, _ => 0.0f);
		Prices = new MarketDataBucket<ProductType.Enums>(productTypes, _ => getRandomInitialPrice());
		PriceHistory = new PriceHistory<ProductType.Enums>();
		recordPriceSnapshot(initialDt);
	}


	private float getRandomInitialPrice()
	{
		double randomValue = Random.Shared.NextDouble();
		return 50.0f + (float)(randomValue * 100.0);
	}


	/// <summary>
	/// 基于供需关系，动态平衡商品价格
	/// </summary>
	public void BalancePrice(string dt)
	{
		List<ProductType.Enums> productTypes = ProductTemplate.GetTemplates().Keys.ToList();
		productTypes.ForEach(productType =>
		{
			float supply = Supplies.Get(productType);
			float demand = ConsumerDemands.Get(productType) + IndustryDemands.Get(productType);

			if (supply == 0.0f && demand == 0.0f)
			{
				return;
			}

			float currentPrice = Prices.Get(productType);
			float adjustment = 0.05f;
			if (demand > supply)
			{
				Prices.Set(productType, MathF.Max(MinProductPrice, currentPrice * (1.0f + adjustment)));
			}
			else if (supply > demand)
			{
				Prices.Set(productType, MathF.Max(MinProductPrice, currentPrice * (1.0f - adjustment)));
			}
		});

		recordPriceSnapshot(dt);

		NotifyChanged();
	}

	/// <summary>
	/// 重置为单条价格历史记录。
	/// </summary>
	public void ResetPriceHistory(string dt)
	{
		PriceHistory.ResetWithSingleSnapshot(dt, captureCurrentPrices());
	}

	/// <summary>
	/// 从存档恢复价格历史。
	/// </summary>
	public void LoadPriceHistory(List<Dictionary<string, object>> historyData)
	{
		PriceHistory.LoadSaveData(historyData);
	}

	private void recordPriceSnapshot(string dt)
	{
		PriceHistory.Record(dt, captureCurrentPrices());
	}

	private Dictionary<ProductType.Enums, float> captureCurrentPrices()
	{
		return ProductTemplate.GetTemplates().Keys.ToDictionary(productType => productType, productType => Prices.Get(productType));
	}

	/// <summary>
	/// 手动触发市场变更通知。
	/// </summary>
	public void NotifyChanged()
	{
		MarketChanged?.Invoke();
	}
}
