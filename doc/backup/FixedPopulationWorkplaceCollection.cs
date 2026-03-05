using System;

/// <summary>
/// 固定人口约束的工作分配集合。
/// </summary>
public partial class FixedPopulationWorkplaceCollection : WorkplaceCollection
{
	// 固定人口对象。
	private readonly Population fixedPopulation;

	/// <summary>
	/// 初始化固定人口工作分配集合。
	/// </summary>
	public FixedPopulationWorkplaceCollection(Population fixedPopulation)
	{
		this.fixedPopulation = fixedPopulation;
	}

	/// <summary>
	/// 校验工作分配是否合法。
	/// </summary>
	protected override void validateWorkplace(Workplace workplace)
	{
		if (workplace.Worker != fixedPopulation)
		{
			throw new InvalidOperationException("Workplace的人口与集合固定人口不一致。");
		}
	}


	protected override void onAdd(Workplace item)
	{
		item.Building.Workforce.Work.items.Add(item);
	}

	protected override void onRemove(Workplace item)
	{
		item.Building.Workforce.Work.items.Remove(item);
	}

}
