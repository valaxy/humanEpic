using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 世界逻辑：人口按效用效率优先消耗消费品资产，并将其转化为需求满足度。
/// </summary>
public class PopulationConsumeConsumerGoodsLogic : WorldLogic
{
	// 人口集合。
	private readonly PopulationCollection populations;
	// 消费品模板缓存。
	private readonly IReadOnlyDictionary<ProductType.Enums, ProductTemplate> consumerGoodTemplates;

	/// <summary>
	/// 初始化人口消费消耗逻辑。
	/// </summary>
	public PopulationConsumeConsumerGoodsLogic(PopulationCollection populations, float intervalDays)
		: base("PopulationConsumeConsumerGoods", "人口按边际效用效率优先消耗资产中的消费品，持续补充需求直至库存耗尽。", intervalDays)
	{
		this.populations = populations;
		consumerGoodTemplates = ProductTemplate.GetConsumerGoodTemplates();
	}

	/// <summary>
	/// 触发时处理全部人口资产消费。
	/// </summary>
	protected override void ProcessLogic()
	{
		populations
			.GetAll()
			.ToList()
			.ForEach(consumePopulationAssets);
	}

	// 处理单个人口的消费行为。
	private void consumePopulationAssets(Population population)
	{
		if (population.Count <= 0)
		{
			return;
		}

		while (true)
		{
			List<(ProductTemplate template, float availableAmount, float utilityEfficiency)> candidates = consumerGoodTemplates
				.Values
				.Select(template =>
				{
					float availableAmount = population.Asset.GetAmount(template.Type);
					float utilityEfficiency = calculateUtilityEfficiency(population, template);
					return (template, availableAmount, utilityEfficiency);
				})
				.Where(item => item.availableAmount > 0.0001f && item.utilityEfficiency > 0.0f)
				.OrderByDescending(item => item.utilityEfficiency)
				.ToList();

			if (candidates.Count == 0)
			{
				return;
			}

			(ProductTemplate template, float availableAmount, float utilityEfficiency) bestCandidate = candidates[0];
			float incrementalAmount = MathF.Max(0.01f, population.Count * MathF.Max(bestCandidate.template.DailyConsumptionSpeed, 0.01f));
			float consumeAmount = MathF.Min(bestCandidate.availableAmount, incrementalAmount);
			population.Asset.ConsumeAmount(bestCandidate.template.Type, consumeAmount);
			applyDemandIncrease(population, bestCandidate.template, consumeAmount);
		}
	}

	// 计算消费 1 单位商品的效用效率。
	private float calculateUtilityEfficiency(Population population, ProductTemplate productTemplate)
	{
		return productTemplate
			.NeedSatisfactionRatios
			.ToList()
			.Select(entry =>
			{
				Demand demand = population.Demands.Get(entry.Key);
				float currentDegree = demand.GetSatisfiedAmountPerPerson(population.Count);
				float nextDegree = (demand.SatisfiedAmount + entry.Value) / population.Count;
				float oldUtility = demand.GetTotalUtility(currentDegree);
				float newUtility = demand.GetTotalUtility(nextDegree);
				return MathF.Max(0.0f, newUtility - oldUtility);
			})
			.Sum();
	}

	// 将消费品转化为需求满足度增量。
	private void applyDemandIncrease(Population population, ProductTemplate productTemplate, float consumeAmount)
	{
		productTemplate
			.NeedSatisfactionRatios
			.ToList()
			.ForEach(entry =>
			{
				Demand demand = population.Demands.Get(entry.Key);
				demand.SatisfiedAmount += entry.Value * consumeAmount;
			});
	}
}
