
using System;

/// <summary>
/// 人口需求，记录某类需求的当前满足值
/// </summary>
[Persistable]
public class Demand
{
	// 需求模板
	private DemandTemplate template = default!;

	[PersistField]
	private float satisfiedAmount = default!;

	/// <summary>
	/// 需求类型
	/// </summary>
	[PersistProperty]
	public DemandType.Enums TypeId
	{
		get => template.Type;
		private set => template = DemandTemplate.GetTemplate(value);
	}




	/// <summary>
	/// 需求名称
	/// </summary>
	public string Name => template.Name;

	/// <summary>
	/// 单人单日需求度耗损量基数系数。
	/// </summary>
	public float PerCapitaDailyDecayBase => template.PerCapitaDailyDecayBase;

	/// <summary>
	/// 已满足需求量，它是一个大于等于0的实数
	/// 但真实情况下，这个数除以总人数会收敛到人均[0, 1]之间
	/// </summary>
	public float SatisfiedAmount
	{
		get => satisfiedAmount;
		set => satisfiedAmount = MathF.Max(0.0f, value);
	}


	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private Demand()
	{
	}

	/// <summary>
	/// 初始化需求对象
	/// </summary>
	public Demand(DemandType.Enums type, float satisfiedAmount)
	{
		TypeId = type;
		SatisfiedAmount = satisfiedAmount;
	}

	/// <summary>
	/// 获取人均满足度。
	/// </summary>
	public float GetSatisfiedAmountPerPerson(int populationCount)
	{
		return SatisfiedAmount / populationCount;
	}

	/// <summary>
	/// 计算需求总效用
	/// </summary>
	public float GetTotalUtility(float demandDegree)
	{
		return template.DemandUtility.GetTotalUtility(demandDegree);
	}

	/// <summary>
	/// 按人口数量执行每日需求度耗损。
	/// </summary>
	public void DecayNaturally(int populationCount)
	{
		float decayAmount = PerCapitaDailyDecayBase * populationCount;
		SatisfiedAmount = SatisfiedAmount - decayAmount;
	}
}
