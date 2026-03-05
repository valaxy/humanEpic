// using Godot;
// using System;
// using System.Collections.Generic;
// using GDictionary = Godot.Collections.Dictionary;

// /// <summary>
// /// 民宅建筑模板，定义民宅的静态配置
// /// </summary>
// [GlobalClass]
// public partial class ResidentialBuildingTemplate : BuildingTemplate, ITemplate<ResidentialBuildingType.Enums, ResidentialBuildingTemplate>
// {
// 	private const string residentialTemplateCsvPath = "res://src/data/residential_building_templates.csv";
// 	private static readonly CsvSchema residentialTemplateSchema = new CsvSchema(
// 		residentialTemplateCsvPath,
// 		new List<CsvColumnDefinition>
// 		{
// 			CsvColumnDefinition.Enum<ResidentialBuildingType.Enums>("type_id"),
// 			CsvColumnDefinition.String("name"),
// 			CsvColumnDefinition.Int("max_population", minInclusive: 0),
// 			CsvColumnDefinition.Color("color"),
// 		});

// 	private static Dictionary<ResidentialBuildingType.Enums, ResidentialBuildingTemplate> templates = new Dictionary<ResidentialBuildingType.Enums, ResidentialBuildingTemplate>();

// 	/// <summary>
// 	/// 唯一标识类型
// 	/// </summary>
// 	public ResidentialBuildingType.Enums TypeId { get; private set; }

 
// 	/// <summary>
// 	/// 初始化民宅建筑模板
// 	/// </summary>
// 	public ResidentialBuildingTemplate(ResidentialBuildingType.Enums id, string name, int maxPop, Color color, Dictionary<ProductType.Enums, int> cost)
// 		: base("residential", (int)id, id.ToString(), name, color, cost)
// 	{
// 		TypeId = id;
// 		MaxPopulation = Mathf.Max(0, maxPop);
// 	}

// 	/// <summary>
// 	/// 获取指定ID的模板
// 	/// </summary>
// 	public static ResidentialBuildingTemplate GetTemplate(ResidentialBuildingType.Enums typeId)
// 	{
// 		return templates[typeId];
// 	}


// 	public new static Dictionary<ResidentialBuildingType.Enums, ResidentialBuildingTemplate> GetTemplates()
// 	{
// 		return templates;
// 	}

// 	public static GDictionary GetTemplates_AsGDictionary()
// 	{
// 		return TemplateUtility.ToGDictionary(templates);
// 	}

// 	public static void Initialize()
// 	{
// 		templates = loadTemplatesFromCsv();
// 	}


// 	private static Dictionary<ResidentialBuildingType.Enums, ResidentialBuildingTemplate> loadTemplatesFromCsv()
// 	{
// 		List<CsvRow> rows = CsvReader.ReadRows(residentialTemplateSchema);
// 		Dictionary<ResidentialBuildingType.Enums, ResidentialBuildingTemplate> result = new Dictionary<ResidentialBuildingType.Enums, ResidentialBuildingTemplate>();
// 		HashSet<ResidentialBuildingType.Enums> enumSet = new HashSet<ResidentialBuildingType.Enums>();

// 		foreach (CsvRow row in rows)
// 		{
// 			ResidentialBuildingType.Enums typeId = row.Get<ResidentialBuildingType.Enums>("type_id");
// 			string name = row.Get<string>("name");
// 			int maxPopulation = row.Get<int>("max_population");
// 			Color color = row.Get<Color>("color");
// 			Dictionary<ProductType.Enums, int> constructionCost = ConstructionCostParser.GetConstructionCost("residential", typeId.ToString());

// 			if (!enumSet.Add(typeId) || result.ContainsKey(typeId))
// 			{
// 				throw new InvalidOperationException($"Residential template data error: duplicate type_id '{typeId}'.");
// 			}

// 			result[typeId] = new ResidentialBuildingTemplate(typeId, name, maxPopulation, color, constructionCost);
// 		}

// 		HashSet<ResidentialBuildingType.Enums> allTypes = new HashSet<ResidentialBuildingType.Enums>((ResidentialBuildingType.Enums[])Enum.GetValues(typeof(ResidentialBuildingType.Enums)));
// 		if (!allTypes.SetEquals(enumSet))
// 		{
// 			throw new InvalidOperationException($"Residential template data error: templates must exactly match ResidentialBuildingType.Enums definitions. File: {residentialTemplateCsvPath}");
// 		}

// 		return result;
// 	}
// }
