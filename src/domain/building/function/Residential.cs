using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// 住房的居住功能
/// </summary>
public class Residential : IInfo, IPersistence<Residential, PopulationCollection>
{
    // 缓存当前居住在该建筑中的人口总数
    private int totalCount = 0;
    // 住了哪些人口
    private Dictionary<int, Population> populations = new();
    // 保存反向引用不对外暴露
    private Building building;


    /// <summary>
    /// 适宜居住人口数
    /// </summary>
    public int OptimalCount { get; }

    /// <summary>
    /// 当前居住总人数。
    /// </summary>
    public int TotalCount => totalCount;


    /// <summary>
    /// 初始化住房组件。
    /// </summary>
    public Residential(Building building, int optimalCount)
    {
        Debug.Assert(optimalCount > 0, "适宜居住人口数必须大于0");
        OptimalCount = optimalCount;
        this.building = building;
    }


    /// <summary>
    /// 添加新的入住人
    /// </summary>
    public void Add(Population population, int count)
    {
        Debug.Assert(count > 0, "迁入人数必须大于0");
        totalCount += count;

        // 人口只要记录一下即可
        if (!populations.ContainsKey(population.Id))
        {
            populations[population.Id] = population;
        }
    }



    /// <summary>
    /// 获取民宅建筑的 UI 信息
    /// </summary>
    public InfoData GetInfoData()
    {
        InfoData basicInfo = new InfoData();
        basicInfo.AddNumber("容量上限", OptimalCount);
        basicInfo.AddNumber("当前居住", TotalCount);
        basicInfo.AddProgress("容量占比", OptimalCount == 0 ? 0.0f : (float)TotalCount / OptimalCount, $"{TotalCount} / {OptimalCount}");

        InfoData data = new InfoData();
        // TODO 通过保留的Population反查每个人口的构成
        throw new NotImplementedException();

        return data;
    }

    /// <summary>
    /// 获取保存数据字典
    /// </summary>
    public Dictionary<string, object> GetSaveData()
    {
        throw new NotImplementedException();
    }


    public static Residential LoadSaveData(Dictionary<string, object> data, PopulationCollection? context = default)
    {
        throw new NotImplementedException();
    }
}
