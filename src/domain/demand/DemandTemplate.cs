using System.Collections.Generic;
using System;
using System.Linq;


/// <summary>
/// 需求模板，定义不同需求类型的共同属性与效用函数
/// </summary>
public class DemandTemplate : ITemplate<DemandType.Enums, DemandTemplate>
{
	/// 需求效用函数
	public IDemandUtility DemandUtility { get; }

	/// <summary>
	/// 需求类型
	/// </summary>
	public DemandType.Enums Type { get; }

	/// <summary>
	/// 需求名称
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 单人单日需求度耗损量基数系数。
	/// </summary>
	public float PerCapitaDailyDecayBase { get; }


	/// <summary>
	/// 初始化需求模板
	/// </summary>
	public DemandTemplate(DemandType.Enums type, string name, IDemandUtility demandUtility, float perCapitaDailyDecayBase)
	{
		Type = type;
		Name = name;
		DemandUtility = demandUtility;
		PerCapitaDailyDecayBase = perCapitaDailyDecayBase;
	}

	/// <summary>
	/// 需求模板 CSV 路径。
	/// </summary>
	private const string CsvPath = "res://src/data/demand_templates.csv";

	// 需求模板集合。
	private static readonly Dictionary<DemandType.Enums, DemandTemplate> templates = loadTemplates();

	// 从 CSV 加载需求模板集合。
	private static Dictionary<DemandType.Enums, DemandTemplate> loadTemplates()
	{
		CsvSchema schema = new CsvSchema(
			CsvPath,
			[
				CsvColumnDefinition.Enum<DemandType.Enums>("type"),
				CsvColumnDefinition.String("utility_type"),
				CsvColumnDefinition.Float("target_degree", 0.001f),
				CsvColumnDefinition.Float("target_utility_ratio", 0.001f, 0.999f),
				CsvColumnDefinition.Float("max_utility", 0.0f),
				CsvColumnDefinition.Float("daily_decay_base", 0.0f),
				CsvColumnDefinition.String("name")
			]);

		return CsvReader
			.ReadRows(schema)
			.Select(row =>
			{
				DemandType.Enums type = row.Get<DemandType.Enums>("type");
				DemandTemplate template = new DemandTemplate(
					type,
					row.Get<string>("name"),
					createDemandUtility(row),
					row.Get<float>("daily_decay_base"));
				return (type, template);
			})
			.ToDictionary(item => item.type, item => item.template);
	}

	// 创建需求效用函数实例。
	private static IDemandUtility createDemandUtility(CsvRow row)
	{
		string utilityType = CsvValueUtility.NormalizeKey(row.Get<string>("utility_type"));
		return utilityType switch
		{
			"SATURATION_SURVIVAL" =>
				new SaturationSurvivalUtilityFunction(
					row.Get<float>("target_degree"),
					row.Get<float>("target_utility_ratio"),
					row.Get<float>("max_utility")),
			_ => throw new InvalidOperationException($"Unsupported demand utility type: {utilityType}")
		};
	}

	/// <summary>
	/// 获取所有需求模板
	/// </summary>
	public static Dictionary<DemandType.Enums, DemandTemplate> GetTemplates() => templates;

	/// <summary>
	/// 获取指定需求模板
	/// </summary>
	public static DemandTemplate GetTemplate(DemandType.Enums type) => templates[type];
}
