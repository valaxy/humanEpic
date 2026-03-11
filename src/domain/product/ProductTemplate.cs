using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 产品模板，定义产品的不变属性
/// </summary>
public partial class ProductTemplate : ITemplate<ProductType.Enums, ProductTemplate>
{
	// 消费品对需求的满足倍率映射（可写内部存储）。
	private readonly Dictionary<DemandType.Enums, float> demandsSatisfactionPerProductNum;

	private const string productTemplateCsvPath = "res://src/data/product_templates.csv";
	private static readonly CsvSchema productTemplateSchema = new CsvSchema(
		productTemplateCsvPath,
		new List<CsvColumnDefinition>
		{
			CsvColumnDefinition.Enum<ProductType.Enums>("product_type_id"),
			CsvColumnDefinition.Boolean("is_consumer_good"),
			CsvColumnDefinition.Float("consume_product_num_per_day", 0.0f),
			CsvColumnDefinition.EnumFloatDictionary<DemandType.Enums>("demands_satisfaction_per_product_num", allowEmpty: true, minValue: 0),
			CsvColumnDefinition.String("name"),
			CsvColumnDefinition.String("description"),
		});

	private static readonly Dictionary<ProductType.Enums, ProductTemplate> templates = loadTemplatesFromCsv();
	private static readonly List<ProductType.Enums> consumerGoods = templates
		.Where(entry => entry.Value.IsConsumerGood)
		.Select(entry => entry.Key)
		.ToList();
	private static readonly Dictionary<ProductType.Enums, ProductTemplate> consumerGoodTemplates = consumerGoods
		.ToDictionary(productType => productType, GetTemplate);

	/// <summary>
	/// 产品类型
	/// </summary>
	public ProductType.Enums Type { get; }

	/// <summary>
	/// 产品名称
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 产品描述
	/// </summary>
	public string Description { get; }

	/// <summary>
	/// 是否属于消费品
	/// </summary>
	public bool IsConsumerGood { get; }

	/// <summary>
	/// 每日消耗速度
	/// </summary>
	public float ConsumeProductNumPerDay { get; }

	/// <summary>
	/// 消费品对需求的满足倍率映射
	/// </summary>
	public IReadOnlyDictionary<DemandType.Enums, float> DemandsSatisfactionPerProductNum => demandsSatisfactionPerProductNum;


	/// <summary>
	/// 创建产品模板
	/// </summary>
	/// <param name="type">产品类型</param>
	/// <param name="name">显示名称</param>
	/// <param name="description">产品描述</param>
	/// <param name="isConsumerGood">是否消费品</param>
	/// <param name="consumeProductNumPerDay">每日消耗商品数量</param>
	/// <param name="demandsSatisfactionPerProductNum">每单位商品提供的需求满足度映射</param>
	public ProductTemplate(ProductType.Enums type, string name, string description, bool isConsumerGood, float consumeProductNumPerDay, Dictionary<DemandType.Enums, float> demandsSatisfactionPerProductNum)
	{
		Type = type;
		Name = name;
		Description = description;
		IsConsumerGood = isConsumerGood;
		ConsumeProductNumPerDay = consumeProductNumPerDay;
		this.demandsSatisfactionPerProductNum = demandsSatisfactionPerProductNum.ToDictionary(item => item.Key, item => item.Value);
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

	/// <summary>
	/// 获取全部消费品类型。
	/// </summary>
	public static IReadOnlyList<ProductType.Enums> GetConsumerGoods() => consumerGoods;

	/// <summary>
	/// 获取消费品模板映射缓存。
	/// </summary>
	public static IReadOnlyDictionary<ProductType.Enums, ProductTemplate> GetConsumerGoodTemplates() => consumerGoodTemplates;



	private static Dictionary<ProductType.Enums, ProductTemplate> loadTemplatesFromCsv()
	{
		Dictionary<string, ProductTemplate> templatesByKey = loadBaseTemplates();
		return templatesByKey.Values.ToDictionary(template => template.Type, template => template);
	}

	private static Dictionary<string, ProductTemplate> loadBaseTemplates()
	{
		List<CsvRow> rows = CsvReader.ReadRows(productTemplateSchema);

		List<(ProductType.Enums Type, ProductTemplate Template)> parsedTemplates = rows
			.Select(row =>
			{
				ProductType.Enums type = row.Get<ProductType.Enums>("product_type_id");
				bool isConsumerGood = row.Get<bool>("is_consumer_good");
				float consumeProductNumPerDay = row.Get<float>("consume_product_num_per_day");
				Dictionary<DemandType.Enums, float> demandsSatisfactionPerProductNum = row.Get<Dictionary<DemandType.Enums, float>>("demands_satisfaction_per_product_num");
				if (!isConsumerGood && demandsSatisfactionPerProductNum.Count > 0)
				{
					throw new InvalidOperationException($"Product template data error: product_type_id '{type}' is not a consumer good, but has demands_satisfaction_per_product_num in {productTemplateCsvPath}.");
				}
				if (!isConsumerGood && consumeProductNumPerDay > 0.0f)
				{
					throw new InvalidOperationException($"Product template data error: product_type_id '{type}' is not a consumer good, but has consume_product_num_per_day > 0 in {productTemplateCsvPath}.");
				}

				ProductTemplate template = new ProductTemplate(
					type,
					row.Get<string>("name"),
					row.Get<string>("description"),
					isConsumerGood,
					consumeProductNumPerDay,
					demandsSatisfactionPerProductNum);
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

}
