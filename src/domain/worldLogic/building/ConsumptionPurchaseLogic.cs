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

    // 可消费商品类型缓存。
    private readonly IReadOnlyList<ProductType.Enums> consumerGoods;
    // 可消费商品模板缓存。
    private readonly IReadOnlyDictionary<ProductType.Enums, ProductTemplate> consumerGoodTemplates;

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
        consumerGoods = ProductTemplate.GetConsumerGoods();
        consumerGoodTemplates = ProductTemplate.GetConsumerGoodTemplates();
    }

    /// <summary>
    /// 处理一次消费购买周期。
    /// </summary>
    protected override void ProcessLogic()
    {
        IReadOnlyList<Population> populations = populationCollection.GetAll();
        if (populations.Count == 0)
        {
            GD.Print("消费购买逻辑: 当前没有人口，跳过本轮消费购买");
            return;
        }

        ProductMarket? productMarket = resolveGlobalMarket();
        if (productMarket == null)
        {
            GD.Print("消费购买逻辑: 未找到全局市场，跳过本轮消费购买");
            return;
        }


        // 1. 计算所有商品的性价比
        Dictionary<ProductType.Enums, float> productValueForMoney = getProductValueForMoney(populations, productMarket);

        // 2. 将性价比映射成购买比例
        Dictionary<ProductType.Enums, float> productBuyRatios = getBuyRatios(productValueForMoney);

        // 3. 每个消费仓库独立执行“花完预算”的购买流程。
        List<(Warehouse Warehouse, IReadOnlyList<(Population Population, int Count)> PopulationEntries)> consumerWarehouses = getConsumerWarehouses();
        Dictionary<ProductType.Enums, float> totalDemandByProduct = consumerGoods.ToDictionary(productType => productType, _ => 0.0f);
        consumerWarehouses.ForEach(entry => consumeOneWarehouseBudget(entry.Warehouse, entry.PopulationEntries, productMarket, productBuyRatios, totalDemandByProduct));

        // 4. 更新消费者对商品的需求量和价格
        publichProductPurchase(productMarket, totalDemandByProduct);
    }

    // 解析当前全局市场（TODO 暂时使用第一个市场建筑）。
    private ProductMarket? resolveGlobalMarket()
    {
        return buildingCollection.GetAll()
            .Select(building => building.Market)
            .Where(market => market != null)
            .Select(market => market!.ProductMarket)
            .FirstOrDefault();
    }

    // 计算所有商品的性价比：边际效用 / 价格。
    private Dictionary<ProductType.Enums, float> getProductValueForMoney(IReadOnlyList<Population> populations, ProductMarket market)
    {
        return consumerGoods.ToDictionary(
            productType => productType,
            productType =>
            {
                ProductTemplate template = consumerGoodTemplates[productType];
                float utility = calculateAverageMarginalUtility(populations, template);
                float price = Mathf.Max(market.Prices.Get(productType), minPrice);
                return utility / price;
            });
    }

    // 计算一个商品在全体人口上的平均边际效用（已合并单人口边际效用计算）。
    private float calculateAverageMarginalUtility(IReadOnlyList<Population> populations, ProductTemplate template)
    {
        return populations
            .Select(population => template.NeedSatisfactionRatios.Sum(ratioEntry => calculateDemandUtilityDelta(population, ratioEntry.Key, ratioEntry.Value)))
            .Average();
    }

    // 将性价比映射成 softmax 购买比例。
    private Dictionary<ProductType.Enums, float> getBuyRatios(Dictionary<ProductType.Enums, float> valueForMoneyMap)
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

    // 获取所有作为消费主体的仓库（当前使用住宅建筑仓库）。
    private List<(Warehouse Warehouse, IReadOnlyList<(Population Population, int Count)> PopulationEntries)> getConsumerWarehouses()
    {
        return buildingCollection.GetAll()
            .Where(building => building.Residential != null)
            .Select(building => (building.Warehouse, building.Residential!.GetPopulationEntries()))
            .ToList();
    }




    // 对单个消费仓库执行一次预算消耗与购买。
    private void consumeOneWarehouseBudget(
        Warehouse warehouse,
        IReadOnlyList<(Population Population, int Count)> populationEntries,
        ProductMarket market,
        Dictionary<ProductType.Enums, float> buyRatios,
        Dictionary<ProductType.Enums, float> totalDemandByProduct)
    {
        populationEntries
            .Where(entry => entry.Count > 0)
            .ToList()
            .ForEach(populationEntry =>
        {
            int populationId = populationEntry.Population.Id;
            float budget = warehouse.GetAmount(ProductType.Enums.CURRENCY, populationId);
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

                warehouse.AddProduct(productType, boughtAmount, populationId);
                totalDemandByProduct[productType] += boughtAmount;
            });

            warehouse.ConsumeProduct(ProductType.Enums.CURRENCY, budget, populationId);
        });
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


    // 将消费者的购买量回写到市场，供 UI 与后续逻辑读取。
    private void publichProductPurchase(ProductMarket market, Dictionary<ProductType.Enums, float> productPurchase)
    {
        market.ConsumerDemands.Reset();
        productPurchase.ToList().ForEach(demandEntry => market.ConsumerDemands.Set(demandEntry.Key, demandEntry.Value));
        productPurchase.ToList().ForEach(entry => GD.Print($"消费购买逻辑: 商品 {entry.Key} 总消费需求量: {entry.Value}"));
        market.BalancePrice(); // 因为改变了需求量，所以需要重新平衡价格
    }
}
