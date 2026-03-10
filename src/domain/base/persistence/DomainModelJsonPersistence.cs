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
	// 值元组元数据键：标记当前对象节点是值元组包装结构。
	private const string tupleTag = "__tuple";
	// 值元组元数据键：值序列键名。
	private const string tupleItems = "items";


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
		return serializePersistableObject(model, modelType);
	}

	/// <summary>
	/// 从中间字典对象加载模型。
	/// </summary>
	public static TModel LoadFromObject<TModel>(Dictionary<string, object> data) where TModel : class
	{
		Type modelType = typeof(TModel);
		ensurePersistableClass(modelType);
		return (TModel)deserializePersistableObject(data, modelType);
	}
}
