using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	// 字典元数据键：标记当前对象节点是字典包装结构。
	private const string dictTag = "__dict";
	// 字典元数据键：字典条目列表键名。
	private const string dictEntries = "entries";
	// 字典元数据键：条目 key 键名。
	private const string entryKey = "k";
	// 字典元数据键：条目 value 键名。
	private const string entryValue = "v";
	// 集合元数据键：标记当前对象节点是集合包装结构。
	private const string setTag = "__set";
	// 集合元数据键：集合元素列表键名。
	private const string setItems = "items";
	// 值元组元数据键：标记当前对象节点是值元组包装结构。
	private const string tupleTag = "__tuple";
	// 值元组元数据键：值序列键名。
	private const string tupleItems = "items";
	// Godot 值类型元数据键：标记当前对象节点是 Godot 值类型包装结构。
	private const string godotTag = "__godot";
	// Godot 值类型元数据键：类型名。
	private const string godotType = "type";
	// Godot 值类型元数据键：数据体。
	private const string godotData = "data";
	// 根节点静态成员键：按类型分组保存静态字段/属性。
	private const string staticMembers = "__static";


	/// <summary>
	/// 将模型保存为 JSON 字符串。
	/// </summary>
	public static string Save<TModel>(TModel model) where TModel : class
	{
		Dictionary<string, object> data = SaveToObject(model);
		return JsonSerializer.Serialize(data);
	}

	/// <summary>
	/// 将 JSON 字符串加载为模型。
	/// </summary>
	public static TModel Load<TModel>(string json) where TModel : class
	{
		using JsonDocument document = JsonDocument.Parse(json);
		object node = toNativeObject(document.RootElement);
		if (node is not Dictionary<string, object> root)
		{
			throw new InvalidOperationException("根节点必须是对象");
		}

		return LoadFromObject<TModel>(root);
	}

	/// <summary>
	/// 将模型保存为中间字典对象。
	/// </summary>
	public static Dictionary<string, object> SaveToObject<TModel>(TModel model) where TModel : class
	{
		if (model == null)
		{
			throw new InvalidOperationException("持久化模型不能为空");
		}

		Type modelType = model.GetType();
		ensurePersistableClass(modelType);
		return saveRootObject(model, modelType);
	}

	/// <summary>
	/// 从中间字典对象加载模型。
	/// </summary>
	public static TModel LoadFromObject<TModel>(Dictionary<string, object> data) where TModel : class
	{
		Type modelType = typeof(TModel);
		ensurePersistableClass(modelType);
		return (TModel)loadRootObject(data, modelType);
	}
}
