using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 将劳动力和原材料转化为产品或服务
/// </summary>
public class Processing : IInfo
{
    // 每单位产出对应的商品输入配方
    private readonly Dictionary<ProductType.Enums, float> inputs;
    // 每单位产出对应的商品输出配方。
    private readonly Dictionary<ProductType.Enums, float> outputs;


    /// <summary>
    /// 劳动力
    /// </summary>
    public Labour Labour { get; }

    /// <summary>
    /// 可选采集流程；为 null 表示仅加工。
    /// </summary>
    public Harvest? Harvest { get; }

    /// <summary>
    /// 输入配方快照。
    /// </summary>
    public IReadOnlyDictionary<ProductType.Enums, float> Inputs => inputs;

    /// <summary>
    /// 输出配方快照。
    /// </summary>
    public IReadOnlyDictionary<ProductType.Enums, float> Outputs => outputs;

    public Processing(
        Dictionary<ProductType.Enums, float> inputs,
        Dictionary<ProductType.Enums, float> outputs,
        Labour labour,
        Harvest? harvest = null)
    {
        this.inputs = inputs;
        this.outputs = outputs;
        Labour = labour;
        Harvest = harvest;

        // if (this.outputs.Count > 0)
        // {
        //     float min = this.outputs.Values.Min();
        //     Debug.Assert(min == 1.0f, "作为一个硬性要求，输出的最小值必须为1.0f，以确保配比呈现的标准化");
        // }
    }

    /// <summary>
    /// 获取用于 UI 展示的生产流程信息。
    /// </summary>
    public InfoData GetInfoData()
    {
        InfoData formulaInfo = new InfoData();
        if (inputs.Count == 0)
        {
            formulaInfo.AddText("投入配方", "无");
        }
        else
        {
            inputs
                .OrderBy(pair => pair.Key)
                .ToList()
                .ForEach(pair =>
                {
                    ProductTemplate template = ProductTemplate.GetTemplate(pair.Key);
                    formulaInfo.AddNumber($"投入/{template.Name}", pair.Value);
                });
        }

        if (outputs.Count == 0)
        {
            formulaInfo.AddText("产出配方", "无");
        }
        else
        {
            outputs
                .OrderBy(pair => pair.Key)
                .ToList()
                .ForEach(pair =>
                {
                    ProductTemplate template = ProductTemplate.GetTemplate(pair.Key);
                    formulaInfo.AddNumber($"产出/{template.Name}", pair.Value);
                });
        }

        InfoData data = new InfoData();
        data.AddGroup("生产配方", formulaInfo);
        data.AddGroup("劳动力", Labour.GetInfoData());
        if (Harvest != null)
        {
            data.AddGroup("采集参数", Harvest.GetInfoData());
        }

        return data;
    }


    // 先不要删
    // /// <summary>
    // /// 在指定仓库上执行一次加工，返回本轮实际加工单位。
    // /// </summary>
    // public float UpdateProcess(Warehouse warehouse, float productionSpeed, float deltaSeconds)
    // {
    //     if (Harvest != null)
    //     {
    //         return 0.0f;
    //     }

    //     float productionAmount = productionSpeed * deltaSeconds;

    //     foreach (ProductType.Enums inputType in inputs.Keys)
    //     {
    //         float requiredPerUnit = inputs[inputType];
    //         float available = warehouse.GetAmount(inputType);
    //         if (available < requiredPerUnit * productionAmount)
    //         {
    //             productionAmount = available / requiredPerUnit;
    //         }
    //     }

    //     foreach (ProductType.Enums outputType in outputs.Keys)
    //     {
    //         float producedPerUnit = outputs[outputType];
    //         float freeSpace = warehouse.GetRemainingCapacity(outputType);
    //         if (freeSpace < producedPerUnit * productionAmount)
    //         {
    //             productionAmount = freeSpace / producedPerUnit;
    //         }
    //     }

    //     foreach (ProductType.Enums inputType in inputs.Keys)
    //     {
    //         warehouse.ConsumeProduct(inputType, inputs[inputType] * productionAmount);
    //     }

    //     foreach (ProductType.Enums outputType in outputs.Keys)
    //     {
    //         warehouse.AddProduct(outputType, outputs[outputType] * productionAmount);
    //     }

    //     return productionAmount;
    // }


    // // 计算标准配比，单日产出的输入成本。
    // private float calculateInputValue(ProductMarket productMarket) => inputs.Sum(pair => pair.Value * productMarket.Prices.Get(pair.Key));

    // // 计算标准配比，单日产出的输出价值。
    // private float calculateOutputValue(ProductMarket productMarket) => outputs.Sum(pair => pair.Value * productMarket.Prices.Get(pair.Key));

    // // 计算标准配比，单日产出的工资成本
    // private float calculateUnitWageCost(LabourMarket labourMarket) => jobInputs.Sum(pair => pair.Value * labourMarket.JobPrices.Get(pair.Key));




    // // 全量刷新劳动力市场的需求量
    // private void updateLabourMarketDemand(LabourMarket labourMarket)
    // {
    //     throw new NotImplementedException();
    // }




    // public void AddupMarketDemandSupply(ProductMarket productMarket, LabourMarket labourMarket, Workforce workforce)
    // {
    //     // 1. 计算标准配比，单日产出的利润（输出价值 - 输入成本 - 工资成本）。
    //     float unitInputCost = calculateInputValue(productMarket);
    //     float unitWageCost = calculateUnitWageCost(labourMarket);
    //     float unitOutputValue = calculateOutputValue(productMarket);
    //     float profit = unitOutputValue - unitInputCost - unitWageCost;
    // }



    // // 根据利润决定是雇佣更多的员工还是裁员，有利润证明增加工人是有利可图的
    // // 每次只增加当前工人的10%，但总人数不能超过MaxWorkerCount
    // public void UpdateLabours(Workforce workforce)
    // {
    //     // 随机找一组空闲的Popluation，增加到这个workforce里面去，但不能超过最大人口
    //     throw new NotImplementedException();
    // }


    // /// <summary>
    // /// 计算在市场需求限制下的可生产单位上限。
    // /// </summary>
    // public float CalculateDemandLimitedUnits(ProductMarket productMarket)
    // {
    //     List<float> demandUnitCaps = outputs
    //         .Where(pair => pair.Value > 0.0f)
    //         .Select(pair => (productMarket.ConsumerDemands.Get(pair.Key) + productMarket.IndustryDemands.Get(pair.Key)) / pair.Value)
    //         .ToList();

    //     if (demandUnitCaps.Count == 0)
    //     {
    //         return 0.0f;
    //     }

    //     return MathF.Max(0.0f, demandUnitCaps.Min());
    // }


    // // 基于Workforce定义的关系，按工种工资将货币结算到每个人口所在的住宅建筑仓库
    // public void UpdatePayrollToLabours(Workforce workforce)
    // {
    //     throw new NotImplementedException();
    // }



    // /// <summary>
    // /// 按配方权重将最大人数分配到各职业岗位上限。
    // /// </summary>
    // public Dictionary<JobType.Enums, int> BuildWorkforceMaxCounts()
    // {
    //     Dictionary<JobType.Enums, int> result = new Dictionary<JobType.Enums, int>();
    //     if (MaxWorkerCount <= 0 || jobInputs.Count == 0)
    //     {
    //         return result;
    //     }

    //     List<KeyValuePair<JobType.Enums, float>> positiveJobs = jobInputs
    //         .Where(pair => pair.Value > 0.0f)
    //         .ToList();
    //     if (positiveJobs.Count == 0)
    //     {
    //         return result;
    //     }

    //     float totalWeight = positiveJobs.Sum(pair => pair.Value);
    //     if (totalWeight <= 0.0f)
    //     {
    //         int average = MaxWorkerCount / positiveJobs.Count;
    //         int remainder = MaxWorkerCount % positiveJobs.Count;
    //         for (int i = 0; i < positiveJobs.Count; i++)
    //         {
    //             int bonus = i < remainder ? 1 : 0;
    //             result[positiveJobs[i].Key] = average + bonus;
    //         }

    //         return result;
    //     }

    //     Dictionary<JobType.Enums, float> fractions = new Dictionary<JobType.Enums, float>();
    //     int assignedTotal = 0;
    //     foreach (KeyValuePair<JobType.Enums, float> pair in positiveJobs)
    //     {
    //         float exact = MaxWorkerCount * pair.Value / totalWeight;
    //         int assigned = (int)MathF.Floor(exact);
    //         result[pair.Key] = assigned;
    //         fractions[pair.Key] = exact - assigned;
    //         assignedTotal += assigned;
    //     }

    //     int missing = MaxWorkerCount - assignedTotal;
    //     if (missing <= 0)
    //     {
    //         return result;
    //     }

    //     List<KeyValuePair<JobType.Enums, float>> sortedFractions = fractions
    //         .OrderByDescending(pair => pair.Value)
    //         .ToList();

    //     for (int i = 0; i < missing; i++)
    //     {
    //         JobType.Enums jobType = sortedFractions[i % sortedFractions.Count].Key;
    //         result[jobType] = result[jobType] + 1;
    //     }

    //     return result;
    // }
}
