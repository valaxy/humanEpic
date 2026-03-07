using System.Collections.Generic;
using System.Linq;




/// <summary>
/// - 当前市场价格已经确定
/// - 消费者只购买有消费需求且生产量不为0的消费品，将口袋里的钱全部耗尽，获得商品的购买量
/// </summary>
public class MoneyBalanceLogicV1 : WorldLogic
{
    private readonly BuildingCollection buildings;

    public MoneyBalanceLogicV1(BuildingCollection buildings) : base("平衡消费者和生产者口袋里的钱", "", 1f)
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