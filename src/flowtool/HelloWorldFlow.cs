[SystemDynamicsFlow]
public class HelloWorldFlow
{
    /// <summary>
    /// 根据新增人口与离开人口计算总人口变化。
    /// </summary>
    /// <param name="newResidents">新增人口。</param>
    /// <param name="leavingResidents">离开人口。</param>
    /// <returns>人口净变化。</returns>
    [SystemDynamicsProcess]
    public int PopulationDelta(int newResidents, int leavingResidents)
    {
        return newResidents - leavingResidents;
    }

    /// <summary>
    /// 根据人口变化与当前人口得到总人口。
    /// </summary>
    /// <param name="populationDelta">人口净变化。</param>
    /// <param name="basePopulation">基础人口。</param>
    /// <returns>当前总人口。</returns>
    [SystemDynamicsProcess]
    public int Population(int populationDelta, int basePopulation)
    {
        return basePopulation + populationDelta;
    }

    /// <summary>
    /// 根据总人口与就业参与率估算劳动力供给。
    /// </summary>
    /// <param name="population">当前总人口。</param>
    /// <param name="participationRate">就业参与率（百分比整数）。</param>
    /// <returns>劳动力供给。</returns>
    [SystemDynamicsProcess]
    public int LaborSupply(int population, int participationRate)
    {
        return population * participationRate / 100;
    }

    /// <summary>
    /// 根据劳动力供给与资本投入估算产出。
    /// </summary>
    /// <param name="laborSupply">劳动力供给。</param>
    /// <param name="capitalStock">资本存量。</param>
    /// <returns>总产出。</returns>
    [SystemDynamicsProcess]
    public int Output(int laborSupply, int capitalStock)
    {
        return laborSupply * 2 + capitalStock;
    }

    /// <summary>
    /// 根据产出与总人口估算人均消费预算。
    /// </summary>
    /// <param name="output">总产出。</param>
    /// <param name="population">当前总人口。</param>
    /// <returns>人均消费预算。</returns>
    [SystemDynamicsProcess]
    public int ConsumptionBudget(int output, int population)
    {
        int safePopulation = population <= 0 ? 1 : population;
        return output / safePopulation;
    }

    /// <summary>
    /// 根据消费预算与基础价格推导市场需求热度。
    /// </summary>
    /// <param name="consumptionBudget">人均消费预算。</param>
    /// <param name="basePrice">基础价格。</param>
    /// <returns>需求热度。</returns>
    [SystemDynamicsProcess]
    public int DemandIndex(int consumptionBudget, int basePrice)
    {
        int safeBasePrice = basePrice <= 0 ? 1 : basePrice;
        return consumptionBudget * 100 / safeBasePrice;
    }

    /// <summary>
    /// 非流程辅助方法：用于证明未标记方法不会进入 flowtool。
    /// </summary>
    /// <param name="rawValue">原始值。</param>
    /// <returns>仅用于调试的离散值。</returns>
    public int ClampForDebugOnly(int rawValue)
    {
        if (rawValue < 0)
        {
            return 0;
        }

        return rawValue > 100 ? 100 : rawValue;
    }
}