using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 产品模板，定义产品的不变属性
/// </summary>
public partial class ProductTemplate : ITemplate<ProductType.Enums, ProductTemplate>
{
	// 消费品对需求的满足倍率映射（可写内部存储）。
	private readonly Dictionary<DemandType.Enums, float> needSatisfactionRatios;

	private const string productTemplateCsvPath = "res://src/data/product_templates.csv";
	private const string productDemandRatioCsvPath = "res://src/data/product_demand_satisfaction.csv";
	private static readonly CsvSchema productTemplateSchema = new CsvSchema(
		productTemplateCsvPath,
		new List<CsvColumnDefinition>
		{
			CsvColumnDefinition.Enum<ProductType.Enums>("product_type_id"),
			CsvColumnDefinition.String("name"),
			CsvColumnDefinition.Boolean("is_consumer_good"),
			CsvColumnDefinition.Float("volume", minInclusive: 0),
		});

	private static readonly CsvSchema productDemandRatioSchema = new CsvSchema(
		productDemandRatioCsvPath,
		new List<CsvColumnDefinition>
		{
			CsvColumnDefinition.String("product_key"),
			CsvColumnDefinition.Enum<DemandType.Enums>("demand_type_id"),
			CsvColumnDefinition.Float("ratio", minInclusive: 0),
		});

	private static readonly Dictionary<ProductType.Enums, ProductTemplate> templates = loadTemplatesFromCsv();

	/// <summary>
	/// 产品类型
	/// </summary>
	public ProductType.Enums Type { get; }

	/// <summary>
	/// 产品名称
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 是否属于消费品
	/// </summary>
	public bool IsConsumerGood { get; }

	/// <summary>
	/// 单位产品体积
	/// </summary>
	public float Volume { get; }

	/// <summary>
	/// 消费品对需求的满足倍率映射
	/// </summary>
	public IReadOnlyDictionary<DemandType.Enums, float> NeedSatisfactionRatios => needSatisfactionRatios;


	/// <summary>
	/// 创建产品模板
	/// </summary>
	/// <param name="type">产品类型</param>
	/// <param name="name">显示名称</param>
	/// <param name="isConsumerGood">是否消费品</param>
	/// <param name="volume">单位产品体积</param>
	/// <param name="needSatisfactionRatios">需求满足倍率映射</param>
	public ProductTemplate(ProductType.Enums type, string name, bool isConsumerGood, float volume, Dictionary<DemandType.Enums, float> needSatisfactionRatios)
	{
		Type = type;
		Name = name;
		IsConsumerGood = isConsumerGood;
		Volume = volume;
		this.needSatisfactionRatios = needSatisfactionRatios;
	}




	/// <summary>
	/// 获取全部模板映射
	/// </summary>
	/// <returns>键为产品类型的模板字典</returns>
	public static Dictionary<ProductType.Enums, ProductTemplate> GetTemplates() => templates;

	/// <summary>
	/// 根据产品类型获取模板
	/// </summary>
	/// <param name="type">产品类型</param>
	/// <returns>对应的产品模板</returns>
	public static ProductTemplate GetTemplate(ProductType.Enums type) => templates[type];



	private static Dictionary<ProductType.Enums, ProductTemplate> loadTemplatesFromCsv()
	{
		Dictionary<string, ProductTemplate> templatesByKey = loadBaseTemplates();
		loadNeedSatisfactionRelations(templatesByKey);
		return templatesByKey.Values.ToDictionary(template => template.Type, template => template);
	}

	private static Dictionary<string, ProductTemplate> loadBaseTemplates()
	{
		List<CsvRow> rows = CsvReader.ReadRows(productTemplateSchema);

		List<(ProductType.Enums Type, ProductTemplate Template)> parsedTemplates = rows
			.Select(row =>
			{
				ProductType.Enums type = row.Get<ProductType.Enums>("product_type_id");
				ProductTemplate template = new ProductTemplate(
					type,
					row.Get<string>("name"),
					row.Get<bool>("is_consumer_good"),
					row.Get<float>("volume"),
					new Dictionary<DemandType.Enums, float>());
				return (type, template);
			})
			.ToList();

		ProductType.Enums duplicatedType = parsedTemplates
			.GroupBy(item => item.Type)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.FirstOrDefault();
		if (parsedTemplates.Count(item => item.Type.Equals(duplicatedType)) > 1)
		{
			throw new InvalidOperationException($"Product template data error: duplicate product_type_id '{duplicatedType}'.");
		}

		HashSet<ProductType.Enums> enumSet = parsedTemplates.Select(item => item.Type).ToHashSet();
		HashSet<ProductType.Enums> allProductTypes = Enum.GetValues<ProductType.Enums>().ToHashSet();
		if (!allProductTypes.SetEquals(enumSet))
		{
			throw new InvalidOperationException($"Product template data error: product templates must exactly match ProductType.Enums definitions. File: {productTemplateCsvPath}");
		}

		return parsedTemplates.ToDictionary(
			item => CsvValueUtility.NormalizeKey(item.Type.ToString()),
			item => item.Template);
	}

	private static void loadNeedSatisfactionRelations(Dictionary<string, ProductTemplate> templatesByKey)
	{
		List<CsvRow> rows = CsvReader.ReadRows(productDemandRatioSchema);

		List<(string ProductKey, DemandType.Enums DemandType, float Ratio)> relations = rows
			.Select(row =>
			{
				string productKey = CsvValueUtility.NormalizeKey(row.Get<string>("product_key"));
				DemandType.Enums demandType = row.Get<DemandType.Enums>("demand_type_id");
				float ratio = row.Get<float>("ratio");
				return (productKey, demandType, ratio);
			})
			.ToList();

		string unknownProductKey = relations
			.Select(relation => relation.ProductKey)
			.Distinct()
			.FirstOrDefault(productKey => !templatesByKey.ContainsKey(productKey), string.Empty);
		if (!string.IsNullOrEmpty(unknownProductKey))
		{
			throw new InvalidOperationException($"Product relation data error: unknown product_key '{unknownProductKey}' in {productDemandRatioCsvPath}.");
		}

		string nonConsumerGoodProductKey = relations
			.Select(relation => relation.ProductKey)
			.Distinct()
			.FirstOrDefault(productKey => !templatesByKey[productKey].IsConsumerGood, string.Empty);
		if (!string.IsNullOrEmpty(nonConsumerGoodProductKey))
		{
			throw new InvalidOperationException($"Product relation data error: product_key '{nonConsumerGoodProductKey}' is not a consumer good, but has demand ratio in {productDemandRatioCsvPath}.");
		}

		string duplicatedRelation = relations
			.GroupBy(relation => $"{relation.ProductKey}|{relation.DemandType}")
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.FirstOrDefault(string.Empty);
		if (!string.IsNullOrEmpty(duplicatedRelation))
		{
			throw new InvalidOperationException($"Product relation data error: duplicate relation '{duplicatedRelation}'.");
		}

		relations.ForEach(relation =>
			templatesByKey[relation.ProductKey].needSatisfactionRatios[relation.DemandType] = relation.Ratio);
	}

}
