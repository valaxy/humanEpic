// using System;
// using System.Collections.Generic;
// using System.Linq;

// /// <summary>
// /// V1版本经济循环，省略了很多细节，主要平衡货币
// /// 消费者确定商品的消费需求量
// /// 生产者与工厂匹配
// /// 生产者确定商品的工业需求量+产出量
// /// 消费者清空钱包（除非没有任何消费品产出），并分摊到每个商品上
// /// 生产者获得工资：对每个工厂，基于商品产出量计算在需求量中的占比，基于占比计算每个工厂的工资，最后收入平均分配跟生产者
// /// 重新更新市场价格
// /// 这样就完成了一轮经济循环
// /// </summary>
// public class EconomyLogiV1 : WorldLogic
// {
//     // 建筑集合。
//     private readonly BuildingCollection buildings;
//     // 时间系统，用于价格快照时间戳。
//     private readonly TimeSystem timeSystem;
//     // 工业生产统一使用人口 0 的库存桶。
//     private const int systemPopulationId = 0;

//     public EconomyLogiV1(BuildingCollection buildings, TimeSystem timeSystem) : base("平衡消费者和生产者口袋里的钱", "", 1f)
//     {
//         this.buildings = buildings;
//         this.timeSystem = timeSystem;
//     }

//     /// <summary>
//     /// 执行生产逻辑的主入口
//     /// </summary>
//     protected override void ProcessLogic()
//     {
//         List<Building> industries = buildings.GetAll()
//             .Where(building => building.Processing != null)
//             .OrderBy(building => building.Id)
//             .ToList();
//         Building? marketBuilding = buildings.GetAll().FirstOrDefault(building => building.Market != null);
//         if (marketBuilding == null)
//         {
//             return;
//         }

//         LabourMarket labourMarket = marketBuilding.Market!.LabourMarket;
//         ProductMarket productMarket = marketBuilding.Market!.ProductMarket;

//         resetMarketBuckets(labourMarket, productMarket);

//         Dictionary<int, float> producedUnitsByBuildingId = industries
//             .ToDictionary(building => building.Id, building => calculateProducedUnits(building, productMarket));

//         industries
//             .ToList()
//             .ForEach(building => applyBuildingProduction(building, producedUnitsByBuildingId[building.Id], productMarket, labourMarket));

//         industries
//             .ToList()
//             .ForEach(building => settlePayroll(building, producedUnitsByBuildingId[building.Id], productMarket));

//         string dt = $"D{timeSystem.GetDay():D6}H{timeSystem.GetHour():D2}";
//         labourMarket.BalancePrice(dt);
//         productMarket.BalancePrice(dt);
//     }

//     // 重置本轮将重新计算的数据桶。
//     private static void resetMarketBuckets(LabourMarket labourMarket, ProductMarket productMarket)
//     {
//         labourMarket.JobDemands.Reset();
//         labourMarket.JobSupplies.Reset();
//         productMarket.IndustryDemands.Reset();
//         productMarket.Supplies.Reset();
//     }

//     // 计算单建筑本轮可生产单位数。
//     private static float calculateProducedUnits(Building building, ProductMarket productMarket)
//     {
//         Processing processing = building.Processing!;
//         float labourLimitedUnits = calculateLabourLimitedUnits(processing.Labour);
//         if (labourLimitedUnits <= 0.0f)
//         {
//             return 0.0f;
//         }

//         float inputLimitedUnits = calculateInputLimitedUnits(processing.Inputs, building.Warehouse);
//         float demandLimitedUnits = calculateDemandLimitedUnits(processing.Outputs, productMarket);
//         return MathF.Max(0.0f, MathF.Min(labourLimitedUnits, MathF.Min(inputLimitedUnits, demandLimitedUnits)));
//     }

//     // 按岗位填充率限制生产能力。
//     private static float calculateLabourLimitedUnits(Labour labour)
//     {
//         List<float> ratios = labour.GetJobLabours()
//             .Where(entry => entry.JobLabour.MaxPopCount > 0)
//             .Select(entry => (float)entry.JobLabour.TotalPopCount / entry.JobLabour.MaxPopCount)
//             .ToList();
//         if (ratios.Count == 0)
//         {
//             return 0.0f;
//         }

//         return MathF.Max(0.0f, ratios.Min());
//     }

//     // 按库存限制生产能力。
//     private static float calculateInputLimitedUnits(IReadOnlyDictionary<ProductType.Enums, float> inputs, Warehouse warehouse)
//     {
//         List<float> inputCaps = inputs
//             .Where(entry => entry.Value > 0.0f)
//             .Select(entry => warehouse.GetAmount(entry.Key, systemPopulationId) / entry.Value)
//             .ToList();
//         if (inputCaps.Count == 0)
//         {
//             return float.MaxValue;
//         }

//         return MathF.Max(0.0f, inputCaps.Min());
//     }

//     // 按市场需求限制生产能力。
//     private static float calculateDemandLimitedUnits(IReadOnlyDictionary<ProductType.Enums, float> outputs, ProductMarket productMarket)
//     {
//         List<float> demandCaps = outputs
//             .Where(entry => entry.Value > 0.0f)
//             .Select(entry =>
//             {
//                 float totalDemand = productMarket.ConsumerDemands.Get(entry.Key) + productMarket.IndustryDemands.Get(entry.Key);
//                 return totalDemand / entry.Value;
//             })
//             .ToList();
//         if (demandCaps.Count == 0)
//         {
//             return 0.0f;
//         }

//         return MathF.Max(0.0f, demandCaps.Min());
//     }

//     // 回写建筑生产结果到市场与仓库，并登记劳动力供需。
//     private static void applyBuildingProduction(Building building, float producedUnits, ProductMarket productMarket, LabourMarket labourMarket)
//     {
//         Processing processing = building.Processing!;
//         processing.Labour.GetJobLabours()
//             .ToList()
//             .ForEach(entry =>
//             {
//                 labourMarket.JobDemands.Add(entry.JobType, entry.JobLabour.MaxPopCount);
//                 labourMarket.JobSupplies.Add(entry.JobType, entry.JobLabour.TotalPopCount);
//             });

//         if (producedUnits <= 0.0f)
//         {
//             return;
//         }

//         processing.Inputs
//             .Where(entry => entry.Value > 0.0f)
//             .ToList()
//             .ForEach(entry =>
//             {
//                 float consumedAmount = entry.Value * producedUnits;
//                 building.Warehouse.ConsumeProduct(entry.Key, consumedAmount, systemPopulationId);
//                 productMarket.IndustryDemands.Add(entry.Key, consumedAmount);
//             });

//         processing.Outputs
//             .Where(entry => entry.Value > 0.0f)
//             .ToList()
//             .ForEach(entry =>
//             {
//                 float producedAmount = entry.Value * producedUnits;
//                 building.Warehouse.AddProduct(entry.Key, producedAmount, systemPopulationId);
//                 productMarket.Supplies.Add(entry.Key, producedAmount);
//             });
//     }

//     // 结算工资，按工厂在需求中的产出占比分配销售额并均分到劳动力人口。
//     private static void settlePayroll(Building building, float producedUnits, ProductMarket productMarket)
//     {
//         if (producedUnits <= 0.0f)
//         {
//             return;
//         }

//         Processing processing = building.Processing!;
//         float payroll = processing.Outputs
//             .Where(entry => entry.Value > 0.0f)
//             .Select(entry =>
//             {
//                 float productDemand = productMarket.ConsumerDemands.Get(entry.Key) + productMarket.IndustryDemands.Get(entry.Key);
//                 float productSupply = entry.Value * producedUnits;
//                 float demandSafe = MathF.Max(productDemand, ProductMarket.MinProductPrice);
//                 float supplyRatioInDemand = MathF.Min(1.0f, productSupply / demandSafe);
//                 float soldAmount = productDemand * supplyRatioInDemand;
//                 float price = MathF.Max(productMarket.Prices.Get(entry.Key), ProductMarket.MinProductPrice);
//                 return soldAmount * price;
//             })
//             .Sum();
//         if (payroll <= 0.0f)
//         {
//             return;
//         }

//         List<(Population Population, int Count)> workerEntries = processing.Labour.GetJobLabours()
//             .SelectMany(entry => entry.JobLabour.GetPopulationEntries())
//             .Where(entry => entry.Count > 0)
//             .ToList();
//         int totalWorkerCount = workerEntries.Sum(entry => entry.Count);
//         if (totalWorkerCount <= 0)
//         {
//             return;
//         }

//         float wagePerWorker = payroll / totalWorkerCount;
//         workerEntries
//             .ToList()
//             .ForEach(entry =>
//             {
//                 float wage = wagePerWorker * entry.Count;
//                 building.Warehouse.AddProduct(ProductType.Enums.CURRENCY, wage, entry.Population.Id);
//             });
//     }
// }