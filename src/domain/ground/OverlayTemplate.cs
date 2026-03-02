using System.Collections.Generic;
using System;
using System.Linq;
using Godot;


/// <summary>
/// 覆盖物模板类，定义覆盖物的不变属性
/// </summary>
public class OverlayTemplate : ITemplate<OverlayType.Enums, OverlayTemplate>
{
	/// <summary>
	/// 覆盖物类型
	/// </summary>
	public OverlayType.Enums Type { get; }

	/// <summary>
	/// 覆盖物名称
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 覆盖物颜色
	/// </summary>
	public Color Color { get; }

	/// <summary>
	/// 最大资源容量
	/// </summary>
	public float MaxAmount { get; }

	/// <summary>
	/// 可放置的地块类型列表
	/// </summary>
	public List<SurfaceType.Enums> ValidGround { get; }


	/// <summary>
	/// 初始化覆盖物模板
	/// </summary>
	public OverlayTemplate(OverlayType.Enums type, string name, Color color, float maxAmount, List<SurfaceType.Enums> validGround)
	{
		Type = type;
		Name = name;
		Color = color;
		MaxAmount = maxAmount;
		ValidGround = validGround;
	}

	/// <summary>
	/// 覆盖物模板 CSV 路径。
	/// </summary>
	private const string CsvPath = "res://src/data/overlay_templates.csv";

	private static readonly Dictionary<OverlayType.Enums, OverlayTemplate> templates = loadTemplates();

	// 从 CSV 加载覆盖物模板集合。
	private static Dictionary<OverlayType.Enums, OverlayTemplate> loadTemplates()
	{
		CsvSchema schema = new CsvSchema(
			CsvPath,
			[
				CsvColumnDefinition.Enum<OverlayType.Enums>("type"),
				CsvColumnDefinition.String("name"),
				CsvColumnDefinition.Color("color"),
				CsvColumnDefinition.Float("max_amount", 0.0f),
				CsvColumnDefinition.String("valid_ground")
			]);

		return CsvReader
			.ReadRows(schema)
			.Select(row =>
			{
				OverlayType.Enums type = row.Get<OverlayType.Enums>("type");
				OverlayTemplate template = new OverlayTemplate(
					type,
					row.Get<string>("name"),
					row.Get<Color>("color"),
					row.Get<float>("max_amount"),
					parseValidGround(row.Get<string>("valid_ground"), type, row.LineNumber));
				return (type, template);
			})
			.ToDictionary(item => item.type, item => item.template);
	}

	// 解析可放置地表类型列表。
	private static List<SurfaceType.Enums> parseValidGround(string text, OverlayType.Enums type, int lineNumber)
	{
		return text
			.Split(';', StringSplitOptions.RemoveEmptyEntries)
			.Select(value => value.Trim())
			.Select(value =>
			{
				if (Enum.TryParse(value, true, out SurfaceType.Enums surfaceType))
				{
					return surfaceType;
				}

				throw new InvalidOperationException($"CSV format error in {CsvPath} line {lineNumber}, overlay '{type}': invalid ground type '{value}'.");
			})
			.Distinct()
			.ToList();
	}

	/// <summary>
	/// 获取所有模板
	/// </summary>
	public static Dictionary<OverlayType.Enums, OverlayTemplate> GetTemplates() => templates;

	/// <summary>
	/// 获取指定类型的模板
	/// </summary>
	public static OverlayTemplate GetTemplate(OverlayType.Enums type) => templates[type];



	/// <summary>
	/// 检查某种覆盖物是否可以放置在指定的地表上
	/// </summary>
	/// <param name="surface">地表类型</param>
	/// <param name="overlayType">覆盖物类型</param>
	/// <returns>是否有效</returns>
	public static bool IsValid(SurfaceType.Enums surface, OverlayType.Enums overlayType)
	{
		if (overlayType == OverlayType.Enums.NONE)
		{
			return true;
		}

		OverlayTemplate template = GetTemplate(overlayType);
		return template.ValidGround.Contains(surface);
	}
}
