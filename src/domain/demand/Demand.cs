
using System;

/// <summary>
/// 需求值对象，记录某类需求的人均最大值与当前满足值
/// </summary>
public class Demand
{
	// 需求模板
	private DemandTemplate template { get; }

	/// <summary>
	/// 需求类型
	/// </summary>
	public DemandType.Enums Type => template.Type;

	/// <summary>
	/// 需求名称
	/// </summary>
	public string Name => template.Name;

	/// <summary>
	/// 已满足需求量
	/// </summary>
	public float SatisfiedAmount { get; set; }

	/// <summary>
	/// 单人单日需求度耗损量基数系数。
	/// </summary>
	public float PerCapitaDailyDecayBase => template.PerCapitaDailyDecayBase;


	/// <summary>
	/// 初始化需求对象
	/// </summary>
	public Demand(DemandType.Enums type, float satisfiedAmount)
	{
		template = DemandTemplate.GetTemplate(type);
		SatisfiedAmount = satisfiedAmount;
	}

	/// <summary>
	/// 计算需求总效用
	/// </summary>
	public float CalculateTotalUtility(float demandDegree)
	{
		return template.DemandUtility.CalculateTotalUtility(demandDegree);
	}

	/// <summary>
	/// 按人口数量执行每日需求度耗损。
	/// </summary>
	public void ApplyDailyDecay(int populationCount)
	{
		float decayAmount = PerCapitaDailyDecayBase * populationCount;
		SatisfiedAmount = MathF.Max(0.0f, SatisfiedAmount - decayAmount);
	}
}
