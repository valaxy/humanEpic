using System.Linq;

/// <summary>
/// 每日按需求模板配置耗损人口需求满足度。
/// </summary>
public class PopulationDemandDecayLogic : WorldLogic
{
    // 人口集合。
    private readonly PopulationCollection populations;

    /// <summary>
    /// 初始化人口需求耗损逻辑。
    /// </summary>
    public PopulationDemandDecayLogic(PopulationCollection populations)
        : base("PopulationDemandDecay", "每日按模板固定消耗人口需求满足度。", 1.0f)
    {
        this.populations = populations;
    }

    /// <summary>
    /// 逻辑触发时处理每日需求耗损。
    /// </summary>
    protected override void ProcessLogic()
    {
        populations
            .GetAll()
            .ToList()
            .ForEach(population => population.ConsumeDemandDaily());
    }
}
