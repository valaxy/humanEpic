using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 住房的居住功能，核心数据还是在Population
/// 这里利用一定的算法从PopulationResidential即时计算出需要的数据，数据不是真实的是模拟的
/// </summary>
public class Residential : IInfo, IPersistence<Residential, PopulationCollection>
{
    // 缓存当前居住在该建筑中的人口总数
    private int totalCount = 0;
    // 住了哪些人口（用于反查展示与持久化）
    private HashSet<int> populations = new();
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
    public void Add(int populationId, int count)
    {
        Debug.Assert(count > 0, "迁入人数必须大于0");
        totalCount += count;

        // 人口只要记录一下即可
        if (!populations.Contains(populationId))
        {
            populations.Add(populationId);
        }
    }


    /// <summary>
    /// 从住宅中移除居住人口。
    /// </summary>
    public void Remove(int populationId, int count, bool isEmpty)
    {
        Debug.Assert(count > 0, "迁出人数必须大于0");
        totalCount -= count;
        Debug.Assert(totalCount >= 0, "住宅居住总数不能为负数");

        if (isEmpty)
        {
            // 如果已经没有居民了，就直接清空记录
            populations.Remove(populationId);
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
        data.AddGroup("容量", basicInfo);

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
