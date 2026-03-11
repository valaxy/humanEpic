using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	// 将静态成员快照回填到类型上。
	private static void deserializeStaticMembers(Dictionary<string, object> staticNode)
	{
		staticNode
			.ToList()
			.ForEach(entry =>
			{
				Type modelType = resolvePersistableTypeBySimpleName(entry.Key);

				if (entry.Value is not Dictionary<string, object> typeNode)
				{
					throw new InvalidOperationException($"静态成员节点结构非法: {entry.Key}");
				}

				deserializeStaticMembersForType(typeNode, modelType);
			});
	}

	private static void deserializeStaticMembersForType(Dictionary<string, object> node, Type modelType)
	{
		List<(FieldInfo field, PersistFieldAttribute attr)> staticFields = PersistenceReflectionHelper.getPersistStaticFields(modelType);
		staticFields
			.ToList()
			.ForEach(item =>
			{
				string key = string.IsNullOrWhiteSpace(item.attr.Name) ? item.field.Name : item.attr.Name;
				if (!node.ContainsKey(key))
				{
					throw new InvalidOperationException($"反持久化缺少静态字段: {modelType.FullName}.{item.field.Name}");
				}

				object value = deserializeValue(node[key], item.field.FieldType);
				item.field.SetValue(null, value);
			});

		List<(PropertyInfo property, PersistPropertyAttribute attr)> staticProperties = PersistenceReflectionHelper.getPersistStaticProperties(modelType);
		staticProperties
			.ToList()
			.ForEach(item =>
			{
				string key = string.IsNullOrWhiteSpace(item.attr.Name) ? item.property.Name : item.attr.Name;
				if (!node.ContainsKey(key))
				{
					throw new InvalidOperationException($"反持久化缺少静态属性: {modelType.FullName}.{item.property.Name}");
				}

				Debug.Assert(item.property.SetMethod != null, $"静态属性缺少 setter，无法反持久化: {modelType.FullName}.{item.property.Name}");

				object value = deserializeValue(node[key], item.property.PropertyType);
				item.property.SetValue(null, value);
			});
	}


	// 反序列化字段字典为可持久化对象。
	internal static object deserializePersistableObject(Dictionary<string, object> node, Type modelType)
	{
		activeOwnerTypeStack?.Push(modelType);
		object instance = Activator.CreateInstance(modelType, true)
			?? throw new InvalidOperationException($"类型 {modelType.FullName} 需要可用的无参构造函数");

		try
		{
			List<(FieldInfo field, PersistFieldAttribute attr)> fields = PersistenceReflectionHelper.getPersistFields(modelType);
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

			List<(PropertyInfo property, PersistPropertyAttribute attr)> properties = PersistenceReflectionHelper.getPersistProperties(modelType);
			properties
				.ToList()
				.ForEach(item =>
				{
					string key = string.IsNullOrWhiteSpace(item.attr.Name) ? item.property.Name : item.attr.Name;
					if (!node.ContainsKey(key))
					{
						throw new InvalidOperationException($"反持久化缺少属性: {modelType.FullName}.{item.property.Name}");
					}

					Debug.Assert(item.property.SetMethod != null, $"属性缺少 setter，无法反持久化: {modelType.FullName}.{item.property.Name}");

					object value = deserializeValue(node[key], item.property.PropertyType);
					item.property.SetValue(instance, value);
				});

			return instance;
		}
		finally
		{
			if (activeOwnerTypeStack != null && activeOwnerTypeStack.Count > 0)
			{
				activeOwnerTypeStack.Pop();
			}
		}
	}

	// 按目标类型递归反序列化值。
	internal static object deserializeValue(object rawValue, Type targetType)
	{
		if (rawValue == null)
		{
			bool nullableValueType = Nullable.GetUnderlyingType(targetType) != null;
			bool nullableReferenceType = !targetType.IsValueType;
			if (nullableValueType || nullableReferenceType)
			{
				return null!;
			}

			throw new InvalidOperationException($"类型 {targetType.FullName} 不可为 null");
		}

		Type effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
		ITypePersistence typePersistence = TypePeristenceFactory.GetTypePersistence(effectiveTargetType);
		return typePersistence.Deserialize(rawValue, effectiveTargetType);
	}


	// 对外 LoadFromObject 先还原静态 sidecar，再还原实例。
	private static object loadRootObject(
		Dictionary<string, object> data,
		Type modelType,
		Type ownerType,
		IEnumerable<object> entityCollections,
		bool includeStaticSidecar)
	{
		using PersistenceScope _ = new(ownerType, entityCollections, includeStaticSidecar);
		if (data.TryGetValue(staticMembers, out object? staticRaw) && activeIncludeStaticSidecar)
		{
			if (staticRaw is not Dictionary<string, object> staticNode)
			{
				throw new InvalidOperationException($"根节点静态数据结构非法: {modelType.FullName}");
			}

			deserializeStaticMembers(staticNode);
		}

		return deserializePersistableObject(data, modelType);
	}

	private static Type resolvePersistableTypeBySimpleName(string typeName)
	{
		return PersistenceReflectionHelper.resolvePersistableTypeBySimpleName(typeName);
	}
}
