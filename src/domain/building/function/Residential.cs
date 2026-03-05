using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 住房的居住功能
/// </summary>
public class Residential : IInfo, IPersistence<Residential, PopulationCollection>
{
    // 缓存当前居住在该建筑中的人口总数
    private int totalCount = 0;
    private Dictionary<int, ResidentialPopulation> popRelations = new();

    /// <summary>
    /// 最大居住人口数
    /// </summary>
    public int MaxPopulation { get; }

    /// <summary>
    /// 当前居住总人数。
    /// </summary>
    public int TotalCount => totalCount;

    /// <summary>
    /// 初始化住房组件。
    /// </summary>
    public Residential(int maxPopulation)
    {
        Debug.Assert(maxPopulation > 0, "最大居住人口必须大于0");
        MaxPopulation = maxPopulation;
    }

    /// <summary>
    /// 迁入一个人口到该建筑中，早符合人口数量限制
    /// </summary>
    public void EnterPopulation(Population population, int count)
    {
        Debug.Assert(count > 0, "迁入人数必须大于0");
        Debug.Assert(totalCount + count <= MaxPopulation, "迁入人数不能超过住宅容量");

        int populationId = population.Id;
        if (popRelations.ContainsKey(populationId))
        {
            popRelations[populationId].Increase(count);
        }
        else
        {
            popRelations[populationId] = new ResidentialPopulation(population, count);
        }

        totalCount += count;
    }



    /// <summary>
    /// 获取民宅建筑的 UI 信息
    /// </summary>
    public InfoData GetInfoData()
    {
        InfoData basicInfo = new InfoData();
        basicInfo.AddNumber("容量上限", MaxPopulation);
        basicInfo.AddNumber("当前居住", TotalCount);
        basicInfo.AddProgress("容量占比", MaxPopulation == 0 ? 0.0f : (float)TotalCount / MaxPopulation, $"{TotalCount} / {MaxPopulation}");

        InfoData relationInfo = new InfoData();
        popRelations.Values
            .Select(relation => relation.Pop)
            .ToList()
            .ForEach(pop => relationInfo.AddNumber($"人口#{pop.Id}", popRelations[pop.Id].PopCount));

        InfoData data = new InfoData();
        data.AddGroup("居住概览", basicInfo);
        if (!relationInfo.IsEmpty)
        {
            data.AddGroup("人口构成", relationInfo);
        }

        return data;
    }

    /// <summary>
    /// 获取保存数据字典
    /// </summary>
    public Dictionary<string, object> GetSaveData()
    {
        List<Dictionary<string, object>> relations = popRelations.Values
            .Select(relation => new Dictionary<string, object>
            {
                { "population_id", relation.Pop.Id },
                { "count", relation.PopCount },
            })
            .ToList();

        return new Dictionary<string, object>
        {
            { "max_population", MaxPopulation },
            { "total_count", TotalCount },
            { "relations", relations },
        };
    }


    public static Residential LoadSaveData(Dictionary<string, object> data, PopulationCollection? context = default)
    {
        int maxPopulation = Convert.ToInt32(data["max_population"]);
        Residential residential = new Residential(maxPopulation);

        if (data.ContainsKey("relations"))
        {
            System.Diagnostics.Debug.Assert(context != null, "恢复 Residential 需要 PopulationCollection 上下文");
            PopulationCollection populationCollection = context!;

            List<Dictionary<string, object>> relations = ((List<object>)data["relations"])
                .Select(item => (Dictionary<string, object>)item)
                .ToList();

            residential.popRelations = relations
                .Select(item =>
                {
                    int populationId = Convert.ToInt32(item["population_id"]);
                    int count = Convert.ToInt32(item["count"]);
                    Population pop = populationCollection.Get(populationId);
                    return ResidentialPopulation.LoadSaveData(pop, count);
                })
                .ToDictionary(item => item.Pop.Id, item => item);
        }

        int calculatedTotalCount = residential.popRelations.Values.Sum(item => item.PopCount);
        int storedTotalCount = data.ContainsKey("total_count") ? Convert.ToInt32(data["total_count"]) : calculatedTotalCount;
        residential.totalCount = storedTotalCount;
        Debug.Assert(storedTotalCount == calculatedTotalCount, "住宅存档中的 total_count 与 relations 汇总不一致");

        return residential;
    }
}
