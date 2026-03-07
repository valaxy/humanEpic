using System.Collections.Generic;
using System.Linq;


/// <summary>
/// 工业生产逻辑
/// - 假设劳动力已经分配到位，根据工业建筑当前劳动力的配置来决定原材料消耗和产量
/// - 以当前市场价格为基础，假设利润等于产品全部卖出后的收入，不用考虑成本，将利润平分给所有工人
/// - 更新商品的产出量和消费量，更新市场价格
/// </summary>
public class ProcessingLogicV1 : WorldLogic
{
    private readonly BuildingCollection buildings;

    public ProcessingLogicV1(BuildingCollection buildings) : base("工业生产", "处理所有工厂的原材料消耗和产品产出", 1f)
    {
        this.buildings = buildings;
    }

    /// <summary>
    /// 执行生产逻辑的主入口
    /// </summary>
    protected override void ProcessLogic()
    {
        var industries = buildings.GetAll().Where(b => b.Processing != null);
        var markets = buildings.GetAll().Where(b => b.Market != null);
        Building market = markets.First(); // TODO 暂时只处理一个市场
        LabourMarket labourMarket = market.Market!.LabourMarket;
        ProductMarket productMarket = market.Market!.ProductMarket;

        // TOOD
    }
}