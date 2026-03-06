using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 世界逻辑：消费购买。
/// 每次触发时，居民消费仓库会花费全部货币，按 softmax(边际效用/价格) 购买消费品。
/// </summary>
public class ConsumptionPurchaseLogic : WorldLogic
{
    // 价格下限，避免除零与异常放大。
    private const float minPrice = 0.0001f;

    // 可消费商品缓存。
    private readonly List<ProductType.Enums> consumerGoods;

    // 人口集合，用于计算需求边际效用。
    private readonly PopulationCollection populationCollection;

    // 建筑集合，用于解析市场与消费者仓库。
    private readonly BuildingCollection buildingCollection;

    /// <summary>
    /// 初始化消费购买逻辑。
    /// </summary>
    public ConsumptionPurchaseLogic(PopulationCollection populationCollection, BuildingCollection buildingCollection)
        : base("消费购买", "按边际效用与价格的性价比进行 softmax 分配，居民仓库花完全部预算购买消费品。", 1.0f)
    {
        this.populationCollection = populationCollection;
        this.buildingCollection = buildingCollection;
        consumerGoods = collectConsumerGoods();
    }

    /// <summary>
    /// 处理一次消费购买周期。
    /// </summary>
    protected override void ProcessLogic()
    {
        ProductMarket? market = resolveGlobalMarket();
        if (market == null)
        {
            return;
        }

        Dictionary<ProductType.Enums, float> totalDemandByProduct = createDemandAccumulator();
        List<Warehouse> consumerWarehouses = getConsumerWarehouses();
        if (consumerWarehouses.Count == 0)
        {
            publishConsumerDemands(market, totalDemandByProduct);
            return;
        }

        IReadOnlyList<Population> populations = populationCollection.GetAll();
        Dictionary<ProductType.Enums, float> valueForMoneyMap = calculateValueForMoney(populations, market);
        Dictionary<ProductType.Enums, float> buyRatios = calculateSoftmaxRatios(valueForMoneyMap);

        // 每个消费仓库独立执行“花完预算”的购买流程。
        consumerWarehouses.ForEach(warehouse => consumeOneWarehouseBudget(warehouse, market, buyRatios, totalDemandByProduct));

        logDemandSummary(totalDemandByProduct);
        publishConsumerDemands(market, totalDemandByProduct);
    }

    // 收集所有消费品类型。
    private List<ProductType.Enums> collectConsumerGoods()
    {
        return ProductTemplate.GetTemplates()
            .Where(entry => entry.Value.IsConsumerGood)
            .Select(entry => entry.Key)
            .ToList();
    }

    // 初始化需求累计器。
    private Dictionary<ProductType.Enums, float> createDemandAccumulator()
    {
        return consumerGoods.ToDictionary(productType => productType, _ => 0.0f);
    }

    // 解析当前全局市场（暂时使用第一个市场建筑）。
    private ProductMarket? resolveGlobalMarket()
    {
        return buildingCollection.GetAll()
            .Select(building => building.Market)
            .Where(market => market != null)
            .Select(market => market!.ProductMarket)
            .FirstOrDefault();
    }

    // 获取所有作为消费主体的仓库（当前使用住宅建筑仓库）。
    private List<Warehouse> getConsumerWarehouses()
    {
        return buildingCollection.GetAll()
            .Where(building => building.Residential != null)
            .Select(building => building.Warehouse)
            .ToList();
    }

    // 计算所有商品的性价比：边际效用 / 价格。
    private Dictionary<ProductType.Enums, float> calculateValueForMoney(IReadOnlyList<Population> populations, ProductMarket market)
    {
        return consumerGoods.ToDictionary(
            productType => productType,
            productType =>
            {
                float utility = calculateAverageMarginalUtility(populations, productType);
                float price = Mathf.Max(market.Prices.Get(productType), minPrice);
                return utility / price;
            });
    }

    // 计算一个商品在全体人口上的平均边际效用。
    private float calculateAverageMarginalUtility(IReadOnlyList<Population> populations, ProductType.Enums productType)
    {
        if (populations.Count == 0)
        {
            return 1.0f;
        }

        return populations
            .Select(population => calculateMarginalUtility(population, productType))
            .Average();
    }

    // 计算某人口对某商品的边际效用。
    private float calculateMarginalUtility(Population population, ProductType.Enums productType)
    {
        ProductTemplate template = ProductTemplate.GetTemplate(productType);
        return template.NeedSatisfactionRatios.Sum(ratioEntry => calculateDemandUtilityDelta(population, ratioEntry.Key, ratioEntry.Value));
    }

    // 计算单一需求维度上的效用增量。
    private float calculateDemandUtilityDelta(Population population, DemandType.Enums demandType, float satisfyRatio)
    {
        Demand demand = population.Demands.Get(demandType);
        float oldDegree = Mathf.Clamp(demand.SatisfiedAmount, 0.0f, 1.0f);
        float newDegree = Mathf.Clamp(oldDegree + satisfyRatio, 0.0f, 1.0f);
        float oldUtility = demand.CalculateTotalUtility(oldDegree);
        float newUtility = demand.CalculateTotalUtility(newDegree);
        return Mathf.Max(0.0f, newUtility - oldUtility);
    }

    // 将性价比映射成 softmax 购买比例。
    private Dictionary<ProductType.Enums, float> calculateSoftmaxRatios(Dictionary<ProductType.Enums, float> valueForMoneyMap)
    {
        if (valueForMoneyMap.Count == 0)
        {
            return new Dictionary<ProductType.Enums, float>();
        }

        float maxVfm = valueForMoneyMap.Values.Max();
        if (maxVfm <= 0.0f)
        {
            float uniformRatio = 1.0f / valueForMoneyMap.Count;
            return valueForMoneyMap.Keys.ToDictionary(key => key, _ => uniformRatio);
        }

        // 参数含义：当 A 的性价比是 B 的 2 倍时，期望购买量约为 4 倍。
        float a = 2.0f;
        float b = 4.0f;
        float k = (float)(a * Math.Log(b) / (a - 1.0f));

        Dictionary<ProductType.Enums, float> expMap = valueForMoneyMap.ToDictionary(
            entry => entry.Key,
            entry => (float)Math.Exp(k * (entry.Value / maxVfm)));

        float expSum = expMap.Values.Sum();
        Debug.Assert(expSum > 0.0f, "softmax 分母必须大于 0");
        return expMap.ToDictionary(entry => entry.Key, entry => entry.Value / expSum);
    }

    // 对单个消费仓库执行一次预算消耗与购买。
    private void consumeOneWarehouseBudget(
        Warehouse warehouse,
        ProductMarket market,
        Dictionary<ProductType.Enums, float> buyRatios,
        Dictionary<ProductType.Enums, float> totalDemandByProduct)
    {
        float budget = warehouse.GetAmount(ProductType.Enums.CURRENCY);
        if (budget <= 0.0f)
        {
            return;
        }

        buyRatios.ToList().ForEach(ratioEntry =>
        {
            ProductType.Enums productType = ratioEntry.Key;
            float ratio = ratioEntry.Value;
            float spent = budget * ratio;
            float price = Mathf.Max(market.Prices.Get(productType), minPrice);
            float boughtAmount = spent / price;

            warehouse.AddProduct(productType, boughtAmount);
            totalDemandByProduct[productType] += boughtAmount;
        });

        warehouse.ConsumeProduct(ProductType.Enums.CURRENCY, warehouse.GetAmount(ProductType.Enums.CURRENCY));
    }

    // 将需求回写到市场，供 UI 与后续逻辑读取。
    private void publishConsumerDemands(ProductMarket market, Dictionary<ProductType.Enums, float> totalDemandByProduct)
    {
        market.ConsumerDemands.Reset();
        totalDemandByProduct.ToList().ForEach(demandEntry => market.ConsumerDemands.Set(demandEntry.Key, demandEntry.Value));
    }

    // 打印本轮消费需求汇总，便于调试与观察。
    private void logDemandSummary(Dictionary<ProductType.Enums, float> totalDemandByProduct)
    {
        totalDemandByProduct.ToList().ForEach(entry => GD.Print($"消费购买逻辑: 商品 {entry.Key} 总消费需求量: {entry.Value}"));
    }
}
