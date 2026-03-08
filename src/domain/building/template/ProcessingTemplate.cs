using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 生产流程模板，定义建筑的劳动力配比、投入产出与采集参数。
/// </summary>
public class ProcessingTemplate : ITemplate<BuildingType.Enums, ProcessingTemplate>
{
	// 生产流程模板 CSV 路径。
	private const string CsvPath = "res://src/data/processing_templates.csv";

	// 解析 schema。
	private static readonly CsvSchema schema = new CsvSchema(
		CsvPath,
		[
			CsvColumnDefinition.Enum<BuildingType.Enums>("type_id"),
			CsvColumnDefinition.EnumIntDictionary<JobType.Enums>("job_inputs", allowEmpty: false, minValue: 0),
			CsvColumnDefinition.EnumFloatDictionary<ProductType.Enums>("input_products", allowEmpty: true, minValue: 0.0f),
			CsvColumnDefinition.EnumFloatDictionary<ProductType.Enums>("output_products", allowEmpty: false, minValue: 0.0f),
			CsvColumnDefinition.Enum<OverlayType.Enums>("target_overlay_type"),
			CsvColumnDefinition.String("collection_radius", allowEmpty: true),
			CsvColumnDefinition.String("collection_speed", allowEmpty: true),
		]);

	// 模板集合。
	private static readonly Dictionary<BuildingType.Enums, ProcessingTemplate> templates = loadTemplates();

	/// <summary>
	/// 建筑类型。
	/// </summary>
	public BuildingType.Enums TypeId { get; }

	/// <summary>
	/// 职业最大人口配置。
	/// </summary>
	public IReadOnlyDictionary<JobType.Enums, int> JobInputs { get; }

	/// <summary>
	/// 投入产品配方。
	/// </summary>
	public IReadOnlyDictionary<ProductType.Enums, float> InputProducts { get; }

	/// <summary>
	/// 产出产品配方。
	/// </summary>
	public IReadOnlyDictionary<ProductType.Enums, float> OutputProducts { get; }

	/// <summary>
	/// 采集目标覆盖物类型。
	/// </summary>
	public OverlayType.Enums TargetOverlayType { get; }

	/// <summary>
	/// 采集半径。
	/// </summary>
	public float CollectionRadius { get; }

	/// <summary>
	/// 采集速度（预留字段）。
	/// </summary>
	public float CollectionSpeed { get; }

	/// <summary>
	/// 初始化生产流程模板。
	/// </summary>
	public ProcessingTemplate(
		BuildingType.Enums typeId,
		Dictionary<JobType.Enums, int> jobInputs,
		Dictionary<ProductType.Enums, float> inputProducts,
		Dictionary<ProductType.Enums, float> outputProducts,
		OverlayType.Enums targetOverlayType,
		float collectionRadius,
		float collectionSpeed)
	{
		TypeId = typeId;
		JobInputs = new Dictionary<JobType.Enums, int>(jobInputs);
		InputProducts = new Dictionary<ProductType.Enums, float>(inputProducts);
		OutputProducts = new Dictionary<ProductType.Enums, float>(outputProducts);
		TargetOverlayType = targetOverlayType;
		CollectionRadius = collectionRadius;
		CollectionSpeed = collectionSpeed;

		Debug.Assert(OutputProducts.Count > 0, "生产模板至少要有一个产出产品");
		if (TargetOverlayType != OverlayType.Enums.NONE)
		{
			Debug.Assert(CollectionRadius > 0.0f, "采集型流程的采集半径必须大于0");
		}
	}

	/// <summary>
	/// 获取全部生产流程模板。
	/// </summary>
	public static Dictionary<BuildingType.Enums, ProcessingTemplate> GetTemplates() => templates;

	/// <summary>
	/// 获取指定建筑类型的生产流程模板。
	/// </summary>
	public static ProcessingTemplate GetTemplate(BuildingType.Enums typeId) => templates[typeId];

	/// <summary>
	/// 是否存在指定建筑类型的生产流程模板。
	/// </summary>
	public static bool HasTemplate(BuildingType.Enums typeId) => templates.ContainsKey(typeId);

	/// <summary>
	/// 基于模板创建运行期 Processing 实例。
	/// </summary>
	public Processing CreateProcessing()
	{
		Dictionary<JobType.Enums, int> maxCounts = JobInputs
			.Where(item => item.Value > 0)
			.ToDictionary(item => item.Key, item => item.Value);
		Labour labour = new Labour(maxCounts);
		Harvest? harvest = TargetOverlayType != OverlayType.Enums.NONE
			? new Harvest(TargetOverlayType, CollectionRadius)
			: null;

		return new Processing(
			new Dictionary<ProductType.Enums, float>(InputProducts.ToDictionary(item => item.Key, item => item.Value)),
			new Dictionary<ProductType.Enums, float>(OutputProducts.ToDictionary(item => item.Key, item => item.Value)),
			labour,
			harvest);
	}

	// 从 CSV 读取并校验生产模板。
	private static Dictionary<BuildingType.Enums, ProcessingTemplate> loadTemplates()
	{
		List<ProcessingTemplate> loadedTemplates = CsvReader.ReadRows(schema)
			.Select(row =>
			{
				OverlayType.Enums targetOverlayType = row.Get<OverlayType.Enums>("target_overlay_type");
				bool hasHarvest = targetOverlayType != OverlayType.Enums.NONE;

				float collectionRadius = parseHarvestFloatColumn(
					row.Get<string>("collection_radius"),
					hasHarvest,
					"collection_radius",
					row.LineNumber);

				float collectionSpeed = parseHarvestFloatColumn(
					row.Get<string>("collection_speed"),
					hasHarvest,
					"collection_speed",
					row.LineNumber);

				return new ProcessingTemplate(
					row.Get<BuildingType.Enums>("type_id"),
					row.Get<Dictionary<JobType.Enums, int>>("job_inputs"),
					row.Get<Dictionary<ProductType.Enums, float>>("input_products"),
					row.Get<Dictionary<ProductType.Enums, float>>("output_products"),
					targetOverlayType,
					collectionRadius,
					collectionSpeed);
			})
			.ToList();

		BuildingType.Enums duplicatedType = loadedTemplates
			.GroupBy(template => template.TypeId)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.FirstOrDefault();
		if (loadedTemplates.Count(template => template.TypeId.Equals(duplicatedType)) > 1)
		{
			throw new InvalidOperationException($"Processing template data error: duplicate type '{duplicatedType}'. File: {CsvPath}");
		}

		return loadedTemplates.ToDictionary(template => template.TypeId, template => template);
	}

	// 解析采集参数：有 Harvest 时必须填写正数，无 Harvest 时必须留空。
	private static float parseHarvestFloatColumn(string rawValue, bool hasHarvest, string columnName, int lineNumber)
	{
		string normalizedText = rawValue.Trim();

		if (!hasHarvest)
		{
			if (!string.IsNullOrWhiteSpace(normalizedText))
			{
				throw new InvalidOperationException($"Processing template data error: column '{columnName}' must be empty when target_overlay_type is NONE. line={lineNumber}, file={CsvPath}");
			}

			return 0.0f;
		}

		if (string.IsNullOrWhiteSpace(normalizedText))
		{
			throw new InvalidOperationException($"Processing template data error: column '{columnName}' is required when Harvest exists. line={lineNumber}, file={CsvPath}");
		}

		if (!float.TryParse(normalizedText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
		{
			throw new InvalidOperationException($"Processing template data error: column '{columnName}' must be float, got '{rawValue}'. line={lineNumber}, file={CsvPath}");
		}

		if (parsedValue <= 0.0f)
		{
			throw new InvalidOperationException($"Processing template data error: column '{columnName}' must be > 0 when Harvest exists. line={lineNumber}, file={CsvPath}");
		}

		return parsedValue;
	}
}
