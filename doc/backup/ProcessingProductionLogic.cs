using System.Collections.Generic;
using System.Linq;


/// <summary>
/// 工业生产逻辑，负责遍历所有工业建筑并处理其生产过程
/// </summary>
public partial class ProcessingProductionLogic : WorldLogic
{
    private readonly BuildingCollection buildings;

    // 必须一天运行一次，保证逻辑正确
    public ProcessingProductionLogic(BuildingCollection buildings) : base("工业生产", "处理所有工厂的原材料消耗和产品产出", 1f)
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

        // 1. 遍历所有工业建筑，更新劳动力市场的供需数据
        labourMarket.JobDemands.Reset();
        foreach (IndustryBuilding building in industries)
        {
            building.Processing!.AddupMarketDemandSupply(productMarket, labourMarket, building.Workforce);
        }

        // 2. 遍历所有工业建筑，根据利润情况执行决策（招人或解雇）
        labourMarket.BalancePrice();
        foreach (IndustryBuilding building in industries)
        {
            building.Processing!.UpdateLabours(market.Workforce);
        }

        // 3. 遍历所有工业建筑，执行生产，产出产品
        productMarket.Supplies.Reset();
        productMarket.IndustryDemands.Reset();
        foreach (IndustryBuilding building in industries)
        {
            building.Processing!.UpdateProcess(building.Warehouse, building.Template.ProductionSpeed, 1f);
        }

        // 4. 更新商品市场价格后，将消费量按人口分配给所有工人
        // 因为消费是基于上一个阶段的价格进行的，所以这里不更新价格
        foreach (IndustryBuilding building in industries)
        {
            building.Processing!.UpdatePayrollToLabours(building.Workforce);
        }

        // 5. 最后统一更新市场价格
        productMarket.BalancePrice();
    }
}