using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 地表模板，定义不同地表的不变属性
/// </summary>
public class SurfaceTemplate : ITemplate<SurfaceType.Enums, SurfaceTemplate>
{
	/// <summary>
	/// 地表类型的唯一标识符
	/// </summary>
	public SurfaceType.Enums Type { get; }

	/// <summary>
	/// 地表的显示名称
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 地表的默认显示颜色
	/// </summary>
	public Color Color { get; }

	/// <summary>
	/// 初始化地表模板
	/// </summary>
	public SurfaceTemplate(SurfaceType.Enums type, string name, Color color)
	{
		Type = type;
		Name = name;
		Color = color;
	}


	/// <summary>
	/// 地表模板 CSV 路径。
	/// </summary>
	private const string CsvPath = "res://src/data/surface_templates.csv";

	/// <summary>
	/// 地表模板集合。
	/// </summary>
	private static readonly Dictionary<SurfaceType.Enums, SurfaceTemplate> templates = loadTemplates();

	// 从 CSV 加载地表模板集合。
	private static Dictionary<SurfaceType.Enums, SurfaceTemplate> loadTemplates()
	{
		CsvSchema schema = new CsvSchema(
			CsvPath,
			[
				CsvColumnDefinition.Enum<SurfaceType.Enums>("type"),
				CsvColumnDefinition.String("name"),
				CsvColumnDefinition.Color("color")
			]);

		return CsvReader
			.ReadRows(schema)
			.Select(row =>
			{
				SurfaceType.Enums type = row.Get<SurfaceType.Enums>("type");
				SurfaceTemplate template = new SurfaceTemplate(
					type,
					row.Get<string>("name"),
					row.Get<Color>("color"));
				return (type, template);
			})
			.ToDictionary(item => item.type, item => item.template);
	}


	public static SurfaceTemplate GetTemplate(SurfaceType.Enums type) => templates[type];

	public static Dictionary<SurfaceType.Enums, SurfaceTemplate> GetTemplates() => templates;
}
