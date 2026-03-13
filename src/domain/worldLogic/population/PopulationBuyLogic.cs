using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 世界逻辑：人口的购买计划
/// </summary>
[SystemDynamicsFlow]
public class PopulationBuyLogic : WorldLogic
{
	// 人口集合。
	private readonly PopulationCollection populations;
	// 时间系统。
	private readonly TimeSystem timeSystem;

	/// <summary>
	/// 初始化人口消费消耗逻辑。
	/// </summary>
	public PopulationBuyLogic(PopulationCollection populations, TimeSystem timeSystem)
		: base("PopulationConsumeConsumerGoods", "人口按边际效用效率优先消耗资产中的消费品，持续补充需求直至库存耗尽。", 0.1f)
	{
		this.populations = populations;
		this.timeSystem = timeSystem;
	}


	protected override void ProcessLogic()
	{

	}



	[SystemDynamicsProcess]
	private ProductMarket productMarket()
	{
		throw new NotImplementedException();
	}


	/// <summary>
	/// 本轮预期商品购买数量
	/// </summary>
	/// <param name="market"></param>
	/// <param name="budgetMoney"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	[SystemDynamicsProcess]
	private Dictionary<ProductType.Enums, float> expectBuyProductsNum(ProductMarket market, float budgetMoney)
	{
		throw new NotImplementedException();
	}


	private void xx()
	{

	}


}
