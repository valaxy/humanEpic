using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 民宅扩展模板，仅保存具备居住功能建筑的公共配置。
/// </summary>
public class ResidentialTemplate : ITemplate<BuildingType.Enums, ResidentialTemplate>
{
	private const string CsvPath = "res://src/data/building_residential_templates.csv";

	private static readonly CsvSchema schema = new CsvSchema(
		CsvPath,
		[
			CsvColumnDefinition.Enum<BuildingType.Enums>("type"),
			CsvColumnDefinition.Int("max_population", 1),
		]);

	private static readonly Dictionary<BuildingType.Enums, ResidentialTemplate> templates = loadTemplates();

	/// <summary>
	/// 建筑类型。
	/// </summary>
	public BuildingType.Enums BuildingType { get; }

	/// <summary>
	/// 最大居住人口。
	/// </summary>
	public int MaxPopulation { get; }

	/// <summary>
	/// 初始化民宅模板。
	/// </summary>
	public ResidentialTemplate(BuildingType.Enums buildingType, int maxPopulation)
	{
		BuildingType = buildingType;
		MaxPopulation = maxPopulation;
	}

	/// <summary>
	/// 获取全部民宅模板。
	/// </summary>
	public static Dictionary<BuildingType.Enums, ResidentialTemplate> GetTemplates() => templates;

	/// <summary>
	/// 根据建筑类型获取民宅模板。
	/// </summary>
	public static ResidentialTemplate GetTemplate(BuildingType.Enums key) => templates[key];

	/// <summary>
	/// 是否存在对应建筑类型的民宅模板。
	/// </summary>
	public static bool HasTemplate(BuildingType.Enums key) => templates.ContainsKey(key);

	private static Dictionary<BuildingType.Enums, ResidentialTemplate> loadTemplates()
	{
		List<ResidentialTemplate> loadedTemplates = CsvReader.ReadRows(schema)
			.Select(row => new ResidentialTemplate(
				row.Get<BuildingType.Enums>("type"),
				row.Get<int>("max_population")))
			.ToList();

		BuildingType.Enums duplicatedType = loadedTemplates
			.GroupBy(template => template.BuildingType)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.FirstOrDefault();
		if (loadedTemplates.Count(template => template.BuildingType.Equals(duplicatedType)) > 1)
		{
			throw new InvalidOperationException($"Building residential template data error: duplicate type '{duplicatedType}'. File: {CsvPath}");
		}

		return loadedTemplates.ToDictionary(template => template.BuildingType, template => template);
	}
}
