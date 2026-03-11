using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mock 逻辑：为人口随机补充一些消费品资产，便于消费系统联调。
/// </summary>
public class PopulationRandomConsumerGoodsMockLogic : WorldLogic
{
	// 人口集合。
	private readonly PopulationCollection populations;
	// 随机数生成器。
	private readonly Random random;
	// 消费品类型缓存。
	private readonly IReadOnlyList<ProductType.Enums> consumerGoods;

	/// <summary>
	/// 初始化随机消费品注入逻辑。
	/// </summary>
	public PopulationRandomConsumerGoodsMockLogic(PopulationCollection populations, float intervalDays)
		: base("PopulationRandomConsumerGoodsMock", "调试逻辑：周期性为人口随机添加消费品资产。", intervalDays)
	{
		this.populations = populations;
		random = new Random();
		consumerGoods = ProductTemplate.GetConsumerGoods();
	}

	/// <summary>
	/// 触发时随机为人口添加少量消费品。
	/// </summary>
	protected override void ProcessLogic()
	{
		populations
			.GetAll()
			.ToList()
			.ForEach(population =>
			{
				consumerGoods
					.Select(productType => (productType, roll: random.NextDouble()))
					.Where(item => item.roll <= 0.45)
					.Select(item =>
					{
						float randomPerCapitaAmount = nextFloat(0.05f, 0.4f);
						float addedAmount = randomPerCapitaAmount * population.Count * 10;
						return (item.productType, addedAmount);
					})
					.Where(item => item.addedAmount > 0.0f)
					.ToList()
					.ForEach(item => population.Asset.AddAmount(item.productType, item.addedAmount));
			});
	}

	// 生成指定区间的随机浮点数。
	private float nextFloat(float minValue, float maxValue)
	{
		double ratio = random.NextDouble();
		return (float)(minValue + ratio * (maxValue - minValue));
	}
}
