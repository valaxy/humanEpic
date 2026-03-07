using System.Linq;


/// <summary>
/// 招聘逻辑：
/// 假设招聘过程每次都会全量执行，假设所有人同时失业，并同时开始找工作
/// 所有工厂不管利润是多少都是满额招聘 → 确定需求量
/// 所有劳动力都会按劳动力市场价格来满额应聘 → 确定供给量
/// 调整劳动力价格
/// 按新的劳动力价格来来确定工厂和劳动力的分配，需要一定的随机分配算法
/// </summary>
public class RecruitmentLogicV1 : WorldLogic
{
    private readonly BuildingCollection buildings;

    // 必须一天运行一次，保证逻辑正确
    public RecruitmentLogicV1(BuildingCollection buildings) : base("招聘", "", 1)
    {
        this.buildings = buildings;
    }


    protected override void ProcessLogic()
    {
        var industries = buildings.GetAll().Where(b => b.Processing != null);
        var markets = buildings.GetAll().Where(b => b.Market != null);
        Building market = markets.First(); // TODO 暂时只处理一个市场
        LabourMarket labourMarket = market.Market!.LabourMarket;
        ProductMarket productMarket = market.Market!.ProductMarket;

        // TODO
    }
}