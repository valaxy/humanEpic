using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 劳动力市场表组件。
/// </summary>
[GlobalClass]
public partial class LabourMarketTableUI : DataTableView
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
				float wage = labourMarket.GetPrice(jobType);
				float demand = labourMarket.GetDemand(jobType);
				float supply = labourMarket.GetSupply(jobType);
				return new List<string>
				{
					template.Name,
					$"{wage:0.00}",
					$"{demand:0.00}",
					$"{supply:0.00}"
				};
			})
			.ToList();

		List<string> headers = ["职业", "价格", "需求量", "供应量"];
		List<DataTextAlignment> alignments =
		[
			DataTextAlignment.Left,
			DataTextAlignment.Right,
			DataTextAlignment.Right,
			DataTextAlignment.Right
		];

		DataSource source = DataTableDataSourceFactory.Create("劳动力市场", headers, rows);
		DataTable table = DataTable.Create("劳动力市场", alignments, alignments, sortableColumns: [0, 1, 2, 3]);
		Render(source, table);
	}
}
