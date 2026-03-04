using Godot;
using System.Collections.Generic;


/// <summary>
/// 建筑模板基类，定义所有建筑共有的静态配置
/// </summary>
public abstract class BuildingTemplate : ITemplate<string, BuildingTemplate>
{
	// Name作为Key，保存所有的Template
	private static readonly Dictionary<string, BuildingTemplate> templates = new Dictionary<string, BuildingTemplate>();

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
	public Dictionary<ProductType.Enums, float> ConstructionCost { get; }

	/// <summary>
	/// 建筑种类标识字符串
	/// </summary>
	public abstract string Kind { get; }

	/// <summary>
	/// 建筑类型枚举值，Enum Key
	/// </summary>
	public int TypeIdValue { get; }

	/// <summary>
	/// 建筑类型名称，Enum Value，Kind + TypeIdValue + TypeName唯一定义一个建筑物
	/// </summary>
	public string TypeName { get; }




	/// <summary>
	/// 基础构造函数，并在构造时注册可全局使用
	/// </summary>
	protected BuildingTemplate(int typeIdValue, string typeName, string name, Color color, Dictionary<ProductType.Enums, float> constructionCost)
	{
		TypeIdValue = typeIdValue;
		TypeName = typeName;
		Name = name;
		Color = color;
		ConstructionCost = new(constructionCost);
		templates[name] = this; // 全局注册
	}

	/// <summary>
	/// 获取模板实例
	/// </summary>
	public static BuildingTemplate GetTemplate(string name) => templates[name];

	/// <summary>
	/// 获取所有模板实例
	/// </summary>
	public static Dictionary<string, BuildingTemplate> GetTemplates() => templates;
}