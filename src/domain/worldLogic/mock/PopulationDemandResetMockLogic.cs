using System;
using System.Linq;

/// <summary>
/// Mock 逻辑：按固定间隔将所有人口的所有需求重置为人均 1。
/// 仅用于测试和调试。
/// </summary>
public class PopulationDemandResetMockLogic : WorldLogic
{
	// 人口集合。
	private readonly PopulationCollection populations;

	/// <summary>
	/// 初始化人口需求重置 Mock 逻辑。
	/// </summary>
	public PopulationDemandResetMockLogic(PopulationCollection populations, float intervalDays)
		: base("PopulationDemandResetMock", "调试逻辑：周期性将需求重置为人均 1。", intervalDays)
	{
		this.populations = populations;
	}

	/// <summary>
	/// 触发时重置所有人口全部需求度。
	/// </summary>
	protected override void ProcessLogic()
	{
		populations
			.GetAll()
			.ToList()
			.ForEach(population =>
			{
				float resetAmount = Math.Max(population.Count, 0);
				population
					.Demands
					.GetAll()
					.ToList()
					.ForEach(demand => demand.SatisfiedAmount = resetAmount);
			});
	}
}
