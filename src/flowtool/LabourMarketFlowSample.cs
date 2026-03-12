/// <summary>
/// flowtool 示例类：用于验证多类反射解析与布局切换。
/// </summary>
[SystemDynamicsFlow]
public class LabourMarketFlowSample
{
    /// <summary>
    /// 根据岗位需求与薪资水平估算招聘意愿。
    /// </summary>
    /// <param name="jobDemand">岗位需求量。</param>
    /// <param name="wageLevel">薪资水平指数。</param>
    /// <returns>招聘意愿指数。</returns>
    [SystemDynamicsProcess]
    public int RecruitmentIntent(int jobDemand, int wageLevel)
    {
        return jobDemand + (wageLevel / 2);
    }

    /// <summary>
    /// 根据招聘意愿与技能匹配度估算录用数量。
    /// </summary>
    /// <param name="recruitmentIntent">招聘意愿指数。</param>
    /// <param name="skillMatchRate">技能匹配率（百分比整数）。</param>
    /// <returns>录用数量。</returns>
    [SystemDynamicsProcess]
    public int HiringCount(int recruitmentIntent, int skillMatchRate)
    {
        return recruitmentIntent * skillMatchRate / 100;
    }

    /// <summary>
    /// 根据录用数量与离职数量估算就业净变化。
    /// </summary>
    /// <param name="hiringCount">录用数量。</param>
    /// <param name="separationCount">离职数量。</param>
    /// <returns>就业净变化。</returns>
    [SystemDynamicsProcess]
    public int EmploymentDelta(int hiringCount, int separationCount)
    {
        return hiringCount - separationCount;
    }
}
