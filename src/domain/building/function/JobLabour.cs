using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


/// <summary>
/// 专门记录某个职业的人口数量关系
/// </summary>
public class JobLabour : IInfo
{
    // 记录每个人口的劳动力关系
    private Dictionary<int, JobLabourPopulation> labourPopulations = new();

    // 缓存的职业人口数量
    private int totalPopCount = 0;

    /// <summary>
    /// 当前职业的最大人口数量
    /// </summary>
    public int MaxPopCount { get; }

    /// <summary>
    /// 当前职业已分配总人数。
    /// </summary>
    public int TotalPopCount => totalPopCount;

    public JobLabour(int maxPopCount)
    {
        MaxPopCount = maxPopCount;
    }


    /// <summary>
    /// 绑定某个人口到当前职业，初始分配人数可选。
    /// </summary>
    public void RegisterPopulation(Population pop, int initialCount = 0)
    {
        Debug.Assert(initialCount >= 0, "初始人数不能为负数");
        Debug.Assert(initialCount <= pop.UnassignedLabourCount, "初始人数不能超过可分配劳动力人口");
        Debug.Assert(totalPopCount + initialCount <= MaxPopCount, "初始人数不能超过职业的最大人口数量");

        int popId = pop.Id;
        if (labourPopulations.ContainsKey(popId))
        {
            return;
        }

        pop.AddLabour(initialCount);
        labourPopulations[popId] = new JobLabourPopulation
        {
            Pop = pop,
            PopCount = initialCount,
        };
        totalPopCount += initialCount;
    }


    /// <summary>
    /// 增加的人数不能超过：最大人口、剩余劳动力人口
    /// </summary>
    public void Increase(int popId, int count)
    {
        Debug.Assert(labourPopulations.ContainsKey(popId), "当前职业中不存在该人口绑定关系");

        JobLabourPopulation labourPop = labourPopulations[popId];
        Population pop = labourPop.Pop;

        Debug.Assert(count > 0, "增加人数只能是正数");
        Debug.Assert(pop.UnassignedLabourCount >= count, "增加人数不能超过可分配劳动力人口");
        Debug.Assert(totalPopCount + count <= MaxPopCount, "增加人数不能超过职业的最大人口数量");

        pop.AddLabour(count);
        labourPop.PopCount += count;
        labourPopulations[popId] = labourPop;
        totalPopCount += count;
    }

    /// <summary>
    /// 获取用于 UI 展示的职业劳动力信息。
    /// </summary>
    public InfoData GetInfoData()
    {
        InfoData basicInfo = new InfoData();
        basicInfo.AddNumber("职业上限", MaxPopCount);
        basicInfo.AddNumber("已分配", TotalPopCount);
        float progress = MaxPopCount > 0 ? (float)TotalPopCount / MaxPopCount : 0.0f;
        basicInfo.AddProgress("分配占比", progress, $"{TotalPopCount} / {MaxPopCount}");

        InfoData populationInfo = new InfoData();
        labourPopulations
            .Values
            .OrderBy(item => item.Pop.Id)
            .ToList()
            .ForEach(item => populationInfo.AddNumber($"{item.Pop.Name}(#{item.Pop.Id})", item.PopCount));

        InfoData data = new InfoData();
        data.AddGroup("岗位概览", basicInfo);
        if (!populationInfo.IsEmpty)
        {
            data.AddGroup("人口分配", populationInfo);
        }

        return data;
    }

    /// <summary>
    /// 获取当前职业下的人口分配快照。
    /// </summary>
    public IReadOnlyList<(Population Population, int Count)> GetPopulationEntries()
    {
        return labourPopulations
            .Values
            .Select(item => (item.Pop, item.PopCount))
            .ToList();
    }
}



struct JobLabourPopulation
{
    public int PopCount;
    public Population Pop;
}