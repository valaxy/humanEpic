using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

/// <summary>
/// 按天聚合的历史数据容器，只保留最近 N 天。
/// </summary>
[Persistable]
public class DailyHistoryData<T> where T : struct, INumber<T>
{
    // 需要保留的天数窗口。
    [PersistField]
    private int keepDays = default!;

    // 最近一次被写入的天数。
    [PersistField]
    private int latestUpdatedDay = -1;

    // 按天聚合后的值。
    [PersistField]
    private Dictionary<int, T> valuesByDay = new();

    /// <summary>
    /// 无参构造函数，供反持久化调用。
    /// </summary>
    private DailyHistoryData()
    {
    }

    /// <summary>
    /// 创建历史数据容器。
    /// </summary>
    public DailyHistoryData(int keepDays)
    {
        Debug.Assert(keepDays > 0, "保留天数必须大于 0");
        this.keepDays = keepDays;
    }

    /// <summary>
    /// 向当日数据追加值，同一天会自动累加。
    /// </summary>
    public void AddCurrentDayValue(int day, T value)
    {
        Debug.Assert(day >= 0, "天数必须从 0 开始");
        Debug.Assert(day >= latestUpdatedDay, "历史天数已封存，不能回写");

        T currentValue = valuesByDay.TryGetValue(day, out T existingValue)
            ? existingValue
            : T.Zero;
        valuesByDay[day] = currentValue + value;

        latestUpdatedDay = Math.Max(latestUpdatedDay, day);
        trimOldDays(day);
    }

    /// <summary>
    /// 读取指定天数的聚合值，不存在时返回 0。
    /// </summary>
    public T GetValueByDay(int day)
    {
        Debug.Assert(day >= 0, "天数必须从 0 开始");
        return valuesByDay.TryGetValue(day, out T value)
            ? value
            : T.Zero;
    }

    /// <summary>
    /// 读取最近 X 天（含当天）的值序列，不足部分补 0。
    /// </summary>
    public IReadOnlyList<T> GetRecentValues(int currentDay, int recentDays)
    {
        Debug.Assert(currentDay >= 0, "天数必须从 0 开始");
        Debug.Assert(recentDays > 0, "查询天数必须大于 0");

        int startDay = currentDay - recentDays + 1;
        return Enumerable.Range(startDay, recentDays)
            .Select(day => day < 0 ? T.Zero : GetValueByDay(day))
            .ToList();
    }

    /// <summary>
    /// 计算最近 X 天（含当天）的平均值。
    /// </summary>
    public double GetRecentAverage(int currentDay, int recentDays)
    {
        Debug.Assert(recentDays > 0, "查询天数必须大于 0");
        return GetRecentValues(currentDay, recentDays)
            .Average(value => Convert.ToDouble(value));
	}

    // 裁剪窗口外的历史值。
    private void trimOldDays(int currentDay)
    {
        int minDay = currentDay - keepDays + 1;
        valuesByDay.Keys
            .Where(day => day < minDay)
            .ToList()
            .ForEach(day => valuesByDay.Remove(day));
    }
}
