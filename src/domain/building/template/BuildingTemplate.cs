using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// 建筑模板基类，定义所有建筑共有的静态配置
/// </summary>
public class BuildingTemplate : ITemplate<BuildingType.Enums, BuildingTemplate>
{
	// 建筑模板 CSV 路径。
	private const string CsvPath = "res://src/data/building_templates.csv";

	private static readonly CsvSchema templateSchema = new CsvSchema(
		CsvPath,
		[
			CsvColumnDefinition.Enum<BuildingType.Enums>("type"),
			CsvColumnDefinition.String("name"),
			CsvColumnDefinition.Color("color"),
			CsvColumnDefinition.EnumFloatDictionary<ProductType.Enums>("construction_cost", true, 0.0f),
		]);


	private static readonly Dictionary<BuildingType.Enums, BuildingTemplate> templates = loadTemplates();


	/// <summary>
	/// 建筑类型
	/// </summary>
	public BuildingType.Enums TypeId { get; }

	/// <summary>
	/// 建筑显示名称
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 关联的颜色
	/// </summary>
	public Color Color { get; }

	/// <summary>
	/// 建造需要的材料
	/// </summary>
	public IReadOnlyDictionary<ProductType.Enums, float> ConstructionCost { get; }

	/// <summary>
	/// 基础构造函数。
	/// </summary>
	public BuildingTemplate(
		BuildingType.Enums typeId,
		string name,
		Color color,
		Dictionary<ProductType.Enums, float> constructionCost)
	{
		TypeId = typeId;
		Name = name;
		Color = color;
		ConstructionCost = new Dictionary<ProductType.Enums, float>(constructionCost);
	}

	/// <summary>
	/// 获取所有模板实例
	/// </summary>
	public static Dictionary<BuildingType.Enums, BuildingTemplate> GetTemplates() => templates;

	/// <summary>
	/// 根据建筑类型获取模板。
	/// </summary>
	public static BuildingTemplate GetTemplate(BuildingType.Enums type) => templates[type];


	private static Dictionary<BuildingType.Enums, BuildingTemplate> loadTemplates()
	{
		// 1) 读取基础建筑模板 CSV，仅包含所有建筑共享的字段。
		List<CsvRow> baseRows = CsvReader.ReadRows(templateSchema);

		// 2) 将每一行解析为领域模板对象。
		List<BuildingTemplate> loadedTemplates = baseRows
			.Select(row =>
			{
				BuildingType.Enums type = row.Get<BuildingType.Enums>("type");
				string name = row.Get<string>("name");
				Color color = row.Get<Color>("color");
				Dictionary<ProductType.Enums, float> constructionCost = row.Get<Dictionary<ProductType.Enums, float>>("construction_cost");

				return new BuildingTemplate(type, name, color, constructionCost);
			})
			.ToList();

		// 3) 校验 type 唯一性，避免模板映射冲突。
		BuildingType.Enums duplicatedType = loadedTemplates
			.GroupBy(template => template.TypeId)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.FirstOrDefault();
		if (loadedTemplates.Count(template => template.TypeId.Equals(duplicatedType)) > 1)
		{
			throw new InvalidOperationException($"Building template data error: duplicate type '{duplicatedType}'. File: {CsvPath}");
		}

		// 4) 校验 CSV 枚举覆盖完整性，确保每个 BuildingType 都有且仅有一条模板。
		HashSet<BuildingType.Enums> loadedTypes = loadedTemplates.Select(template => template.TypeId).ToHashSet();
		HashSet<BuildingType.Enums> allTypes = Enum.GetValues<BuildingType.Enums>().ToHashSet();
		if (!loadedTypes.SetEquals(allTypes))
		{
			throw new InvalidOperationException($"Building template data error: templates must exactly match BuildingType.Enums definitions. File: {CsvPath}");
		}

		return loadedTemplates.ToDictionary(item => item.TypeId, item => item);
	}
}