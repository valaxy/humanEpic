using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 世界逻辑：人口按效用效率优先消耗消费品资产，并将其转化为需求满足度。
/// </summary>
public class PopulationConsumeConsumerGoodsLogic : WorldLogic
{
	// 人口集合。
	private readonly PopulationCollection populations;
	// 时间系统。
	private readonly TimeSystem timeSystem;

	/// <summary>
	/// 初始化人口消费消耗逻辑。
	/// </summary>
	public PopulationConsumeConsumerGoodsLogic(PopulationCollection populations, TimeSystem timeSystem)
		: base("PopulationConsumeConsumerGoods", "人口按边际效用效率优先消耗资产中的消费品，持续补充需求直至库存耗尽。", 0.1f)
	{
		this.populations = populations;
		this.timeSystem = timeSystem;
	}

	/// <summary>
	/// 触发时处理全部人口资产消费。
	/// </summary>
	protected override void ProcessLogic()
	{
		populations
			.GetAll()
			.ToList()
			.ForEach(population => consumePopulationAssets(population, IntervalDays));
	}

	// 处理单个人口的消费行为，每个人口会有一定的时间来消耗消费品
	private void consumePopulationAssets(Population population, float days)
	{
		Debug.Assert(population.Count > 0);

		while (days > 0)
		{
			float deltaDays = days; // 时间可能用不尽
			consumePopulationAssetsDelta(population, ref deltaDays, out bool isEnd);
			if (isEnd)
			{
				break; // 没有可用的消费品了，直接结束循环
			}
			days -= deltaDays; // 减去已经模拟的时间
		}
	}


	// 处理单个人口的消费行为，单次迭代
	private void consumePopulationAssetsDelta(Population population, ref float deltaDays, out bool isEnd)
	{
		List<AssetItem> candidates = calculateProductsOrderByUtilityEfficiency(population);
		if (candidates.Count == 0)
		{
			isEnd = true; // 没有可用的消费品了，直接返回
			return;
		}

		consumeProductIncreaseDemand(population, candidates, ref deltaDays);
		isEnd = false;
	}

	// 按商品效用效率的顺序排列商品列表，并过滤无效使用
	private List<AssetItem> calculateProductsOrderByUtilityEfficiency(Population population)
	{
		List<AssetItem> candidates =
			population.Asset.GetAll()
			.Where(assetItem => assetItem.Template.IsConsumerGood && assetItem.Amount > 0.0f) // 只考虑消费品且数量大于0的资产
			.Select(assetItem =>
			{
				float availableAmount = assetItem.Amount;
				float utilityEfficiency = calculateUtilityEfficiency(population, assetItem.Template);
				return (assetItem, utilityEfficiency);
			})
			.Where(item => item.utilityEfficiency > 0.0f)
			.OrderByDescending(item => item.utilityEfficiency)
			.Select(item => item.assetItem)
			.ToList();
		return candidates;
	}

	// 商品的效用效率：需求度效用 / 时间
	// 可能返回负数
	private float calculateUtilityEfficiency(Population population, ProductTemplate productTemplate)
	{
		return productTemplate
			.DemandsSatisfactionPerProductNum
			.Sum(entry =>
			{
				float satisfiedDemandPerProductNum = entry.Value;
				float satisfiedDemandPerDay = satisfiedDemandPerProductNum * productTemplate.ConsumeProductNumPerDay;
				Demand demand = population.Demands.Get(entry.Key);
				return satisfiedDemandPerDay * demand.GetUtilityDerivative(population.Count); // 乘以导数来扩大需求满足度对效用的贡献
			});
	}

	// 消耗效用效率最高的商品，并将其转化为需求满足度增量。
	private void consumeProductIncreaseDemand(Population population, List<AssetItem> candidates, ref float deltaDays)
	{
		AssetItem bestCandidate = candidates[0]; // 效用效率最高的商品
		float consumeAmount;
		float consumeMaxAmount = deltaDays * bestCandidate.Template.ConsumeProductNumPerDay * population.Count;
		if (consumeMaxAmount < bestCandidate.Amount)
		{
			consumeAmount = consumeMaxAmount;
		}
		else
		{
			consumeAmount = bestCandidate.Amount;
			deltaDays = consumeAmount / (bestCandidate.Template.ConsumeProductNumPerDay * population.Count); // 实际消耗完这个商品需要的时间
		}
		population.Asset.ConsumeAmount(bestCandidate.Template.Type, consumeAmount, timeSystem.GetDay()); // 消费商品
		increaseDemand(population, bestCandidate.Template, consumeAmount); // 提高需求度
	}


	// 将消费品转化为需求满足度增量。
	private void increaseDemand(Population population, ProductTemplate productTemplate, float consumeAmount)
	{
		productTemplate
			.DemandsSatisfactionPerProductNum
			.ToList()
			.ForEach(entry =>
			{
				Demand demand = population.Demands.Get(entry.Key);
				demand.SatisfiedAmount += entry.Value * consumeAmount;
			});
	}
}
