using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 劳动力模型，记录多个职业的人口容量。
/// 每个流程都要确定好职业+职业最大人口
/// </summary>
public class Labour : IInfo
{
	// 记录每个职业不同人口的劳动力关系
	private Dictionary<JobType.Enums, JobLabour> jobLabours = new();

	/// <summary>
	/// 当前总岗位上限。
	/// </summary>
	public int TotalMaxCount => jobLabours.Values.Sum(jobLabour => jobLabour.MaxPopCount);

	/// <summary>
	/// 当前总已分配人数。
	/// </summary>
	public int TotalAssignedCount => jobLabours.Values.Sum(jobLabour => jobLabour.TotalPopCount);


	/// <summary>
	/// 初始化多职业劳动力配置。
	/// </summary>
	public Labour(Dictionary<JobType.Enums, int> maxCount)
	{
		maxCount
			.ToList()
			.ForEach(kvp => jobLabours[kvp.Key] = new JobLabour(kvp.Value));
	}

	/// <summary>
	/// 获取职业劳动力配置快照。
	/// </summary>
	public IReadOnlyList<(JobType.Enums JobType, JobLabour JobLabour)> GetJobLabours()
	{
		return jobLabours
			.Select(entry => (entry.Key, entry.Value))
			.ToList();
	}

	/// <summary>
	/// 获取用于 UI 展示的劳动力信息。
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData basicInfo = new InfoData();
		basicInfo.AddNumber("岗位上限", TotalMaxCount);
		basicInfo.AddNumber("已分配", TotalAssignedCount);
		float progress = TotalMaxCount > 0 ? (float)TotalAssignedCount / TotalMaxCount : 0.0f;
		basicInfo.AddProgress("分配占比", progress, $"{TotalAssignedCount} / {TotalMaxCount}");

		InfoData jobsInfo = new InfoData();
		jobLabours
			.OrderBy(entry => entry.Key)
			.ToList()
			.ForEach(entry =>
			{
				JobTemplate template = JobTemplate.GetTemplate(entry.Key);
				jobsInfo.AddGroup(template.Name, entry.Value.GetInfoData());
			});

		InfoData data = new InfoData();
		data.AddGroup("劳动力概览", basicInfo);
		if (!jobsInfo.IsEmpty)
		{
			data.AddGroup("职业分配", jobsInfo);
		}

		return data;
	}
}
