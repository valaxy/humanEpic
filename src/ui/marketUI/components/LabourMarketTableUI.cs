using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 劳动力市场表组件。
/// </summary>
[GlobalClass]
public partial class LabourMarketTableUI : ReusableDataTable
{
	/// <summary>
	/// 根据劳动力市场数据刷新表格。
	/// </summary>
	public void RenderMarket(LabourMarket labourMarket)
	{
		List<List<string>> rows = Enum.GetValues<JobType.Enums>()
			.Select(jobType =>
			{
				JobTemplate template = JobTemplate.GetTemplate(jobType);
				float wage = labourMarket.JobPrices.Get(jobType);
				return new List<string>
				{
					template.Name,
					$"{wage:0.00}"
				};
			})
			.ToList();

		List<string> headers = ["职业", "工资"];
		List<DataTextAlignment> alignments =
		[
			DataTextAlignment.Left,
			DataTextAlignment.Right
		];

		DataSource source = DataSource.CreateTable("职业工资（日）", headers, rows, alignments, alignments);
		Render(source);
	}
}
