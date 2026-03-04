using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 职业模板，定义职业的通用属性
/// </summary>
public class JobTemplate : ITemplate<JobType.Enums, JobTemplate>
{
	// 职业模板 CSV 路径。
	private const string CsvPath = "res://src/data/job_templates.csv";

	// 职业模板集合。
	private static readonly Dictionary<JobType.Enums, JobTemplate> templates = loadTemplates();

	/// <summary>
	/// 职业类型
	/// </summary>
	public JobType.Enums Type { get; }

	/// <summary>
	/// 职业名称
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 初始化职业模板
	/// </summary>
	public JobTemplate(JobType.Enums type, string name)
	{
		Type = type;
		Name = name;
	}

	// 从 CSV 读取职业模板并完成完整性校验。
	private static Dictionary<JobType.Enums, JobTemplate> loadTemplates()
	{
		CsvSchema schema = new CsvSchema(
			CsvPath,
			[
				CsvColumnDefinition.Enum<JobType.Enums>("type"),
				CsvColumnDefinition.String("name")
			]);

		Dictionary<JobType.Enums, JobTemplate> loadedTemplates = CsvReader
			.ReadRows(schema)
			.Select(row =>
			{
				JobType.Enums type = row.Get<JobType.Enums>("type");
				JobTemplate template = new JobTemplate(type, row.Get<string>("name"));
				return (type, template);
			})
			.ToDictionary(item => item.type, item => item.template);

		HashSet<JobType.Enums> loadedTypes = loadedTemplates.Keys.ToHashSet();
		HashSet<JobType.Enums> allTypes = Enum.GetValues<JobType.Enums>().ToHashSet();
		if (!allTypes.SetEquals(loadedTypes))
		{
			throw new InvalidOperationException($"Job template data error: job templates must exactly match JobType.Enums definitions. File: {CsvPath}");
		}

		return loadedTemplates;
	}

	/// <summary>
	/// 获取指定职业类型的模板
	/// </summary>
	public static JobTemplate GetTemplate(JobType.Enums type) => templates[type];

	public static Dictionary<JobType.Enums, JobTemplate> GetTemplates() => templates;
}
