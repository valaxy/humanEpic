using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	// 将可持久化对象序列化为字段字典。
	private static Dictionary<string, object> serializePersistableObject(object model, Type modelType)
	{
		List<(FieldInfo field, PersistFieldAttribute attr)> fields = getPersistFields(modelType);
		Dictionary<string, object> fieldMap = fields.ToDictionary(
			item => string.IsNullOrWhiteSpace(item.attr.Name) ? item.field.Name : item.attr.Name,
			item => serializeValue(item.field.GetValue(model), item.field.FieldType)
		);

		List<(PropertyInfo property, PersistPropertyAttribute attr)> properties = getPersistProperties(modelType);
		Dictionary<string, object> propertyMap = properties.ToDictionary(
			item => string.IsNullOrWhiteSpace(item.attr.Name) ? item.property.Name : item.attr.Name,
			item =>
			{
				if (item.property.GetMethod == null)
				{
					throw new InvalidOperationException($"属性缺少 getter，无法序列化: {modelType.FullName}.{item.property.Name}");
				}

				return serializeValue(item.property.GetValue(model), item.property.PropertyType);
			}
		);

		if (fieldMap.Keys.Intersect(propertyMap.Keys).Any())
		{
			throw new InvalidOperationException($"类型存在重复持久化键: {modelType.FullName}");
		}

		return fieldMap.Concat(propertyMap)
			.ToDictionary(item => item.Key, item => item.Value);
	}

	// 反序列化字段字典为可持久化对象。
	private static object deserializePersistableObject(Dictionary<string, object> node, Type modelType)
	{
		object instance = Activator.CreateInstance(modelType)
			?? throw new InvalidOperationException($"类型 {modelType.FullName} 需要可用的无参构造函数");

		List<(FieldInfo field, PersistFieldAttribute attr)> fields = getPersistFields(modelType);
		fields
			.ToList()
			.ForEach(item =>
			{
				string key = string.IsNullOrWhiteSpace(item.attr.Name) ? item.field.Name : item.attr.Name;
				if (!node.ContainsKey(key))
				{
					throw new InvalidOperationException($"反持久化缺少字段: {modelType.FullName}.{item.field.Name}");
				}

				object value = deserializeValue(node[key], item.field.FieldType);
				item.field.SetValue(instance, value);
			});

		List<(PropertyInfo property, PersistPropertyAttribute attr)> properties = getPersistProperties(modelType);
		properties
			.ToList()
			.ForEach(item =>
			{
				string key = string.IsNullOrWhiteSpace(item.attr.Name) ? item.property.Name : item.attr.Name;
				if (!node.ContainsKey(key))
				{
					throw new InvalidOperationException($"反持久化缺少属性: {modelType.FullName}.{item.property.Name}");
				}

				if (item.property.SetMethod == null)
				{
					throw new InvalidOperationException($"属性缺少 setter，无法反持久化: {modelType.FullName}.{item.property.Name}");
				}

				object value = deserializeValue(node[key], item.property.PropertyType);
				item.property.SetValue(instance, value);
			});

		return instance;
	}
}
