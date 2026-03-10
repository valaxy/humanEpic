using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	[ThreadStatic]
	private static HashSet<Type>? activeStaticTypeCollector;

	[ThreadStatic]
	private static Stack<Type>? activeOwnerTypeStack;

	[ThreadStatic]
	private static Dictionary<Type, object>? activeEntityCollections;

	[ThreadStatic]
	private static bool activeIncludeStaticSidecar;

	private sealed class PersistenceScope : IDisposable
	{
		private readonly HashSet<Type>? previousStaticTypeCollector;
		private readonly Stack<Type>? previousOwnerTypeStack;
		private readonly Dictionary<Type, object>? previousEntityCollections;
		private readonly bool previousIncludeStaticSidecar;

		public PersistenceScope(Type ownerType, IEnumerable<object> entityCollections, bool includeStaticSidecar)
		{
			previousStaticTypeCollector = activeStaticTypeCollector;
			previousOwnerTypeStack = activeOwnerTypeStack;
			previousEntityCollections = activeEntityCollections;
			previousIncludeStaticSidecar = activeIncludeStaticSidecar;

			activeStaticTypeCollector = new HashSet<Type>();
			activeOwnerTypeStack = new Stack<Type>();
			activeOwnerTypeStack.Push(ownerType);
			activeEntityCollections = createEntityCollectionMap(entityCollections);
			activeIncludeStaticSidecar = includeStaticSidecar;
		}

		public void Dispose()
		{
			activeStaticTypeCollector = previousStaticTypeCollector;
			activeOwnerTypeStack = previousOwnerTypeStack;
			activeEntityCollections = previousEntityCollections;
			activeIncludeStaticSidecar = previousIncludeStaticSidecar;
		}
	}

	// 在一次 Save 周期内收集出现过的可持久化类型。
	private static void registerPersistableType(Type type)
	{
		activeStaticTypeCollector?.Add(type);
	}

	// 创建静态成员快照（根节点 sidecar）。
	private static Dictionary<string, object> serializeStaticMembers(HashSet<Type> types)
	{
		return types
			.Select(type => (type, node: serializeStaticMembersForType(type)))
			.Where(item => item.node.Count > 0)
			.ToDictionary(
				item => item.type.AssemblyQualifiedName
					?? throw new InvalidOperationException($"类型缺少程序集限定名: {item.type.FullName}"),
				item => (object)item.node);
	}

	// 将静态成员快照回填到类型上。
	private static void deserializeStaticMembers(Dictionary<string, object> staticNode)
	{
		staticNode
			.ToList()
			.ForEach(entry =>
			{
				Type modelType = Type.GetType(entry.Key)
					?? throw new InvalidOperationException($"无法解析静态成员所属类型: {entry.Key}");

				if (entry.Value is not Dictionary<string, object> typeNode)
				{
					throw new InvalidOperationException($"静态成员节点结构非法: {entry.Key}");
				}

				deserializeStaticMembersForType(typeNode, modelType);
			});
	}

	private static Dictionary<string, object> serializeStaticMembersForType(Type modelType)
	{
		List<(FieldInfo field, PersistFieldAttribute attr)> staticFields = getPersistStaticFields(modelType);
		Dictionary<string, object> staticFieldMap = staticFields.ToDictionary(
			item => string.IsNullOrWhiteSpace(item.attr.Name) ? item.field.Name : item.attr.Name,
			item => serializeValue(item.field.GetValue(null), item.field.FieldType));

		List<(PropertyInfo property, PersistPropertyAttribute attr)> staticProperties = getPersistStaticProperties(modelType);
		Dictionary<string, object> staticPropertyMap = staticProperties.ToDictionary(
			item => string.IsNullOrWhiteSpace(item.attr.Name) ? item.property.Name : item.attr.Name,
			item =>
			{
				if (item.property.GetMethod == null)
				{
					throw new InvalidOperationException($"静态属性缺少 getter，无法序列化: {modelType.FullName}.{item.property.Name}");
				}

				return serializeValue(item.property.GetValue(null), item.property.PropertyType);
			});

		if (staticFieldMap.Keys.Intersect(staticPropertyMap.Keys).Any())
		{
			throw new InvalidOperationException($"类型存在重复静态持久化键: {modelType.FullName}");
		}

		return staticFieldMap.Concat(staticPropertyMap)
			.ToDictionary(item => item.Key, item => item.Value);
	}

	private static void deserializeStaticMembersForType(Dictionary<string, object> node, Type modelType)
	{
		List<(FieldInfo field, PersistFieldAttribute attr)> staticFields = getPersistStaticFields(modelType);
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

		List<(PropertyInfo property, PersistPropertyAttribute attr)> staticProperties = getPersistStaticProperties(modelType);
		staticProperties
			.ToList()
			.ForEach(item =>
			{
				string key = string.IsNullOrWhiteSpace(item.attr.Name) ? item.property.Name : item.attr.Name;
				if (!node.ContainsKey(key))
				{
					throw new InvalidOperationException($"反持久化缺少静态属性: {modelType.FullName}.{item.property.Name}");
				}

				if (item.property.SetMethod == null)
				{
					throw new InvalidOperationException($"静态属性缺少 setter，无法反持久化: {modelType.FullName}.{item.property.Name}");
				}

				object value = deserializeValue(node[key], item.property.PropertyType);
				item.property.SetValue(null, value);
			});
	}

	// 对外 SaveToObject 需要携带根节点静态 sidecar。
	private static Dictionary<string, object> saveRootObject(
		object model,
		Type modelType,
		Type ownerType,
		IEnumerable<object> entityCollections,
		bool includeStaticSidecar)
	{
		using PersistenceScope _ = new(ownerType, entityCollections, includeStaticSidecar);
		Dictionary<string, object> instanceNode = serializePersistableObject(model, modelType);
		if (activeIncludeStaticSidecar)
		{
			Dictionary<string, object> staticNode = serializeStaticMembers(activeStaticTypeCollector ?? new HashSet<Type>());
			instanceNode[staticMembers] = staticNode;
		}

		return instanceNode;
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

	// 将可持久化对象序列化为字段字典。
	internal static Dictionary<string, object> serializePersistableObject(object model, Type modelType)
	{
		registerPersistableType(modelType);
		activeOwnerTypeStack?.Push(modelType);

		try
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
		finally
		{
			if (activeOwnerTypeStack != null && activeOwnerTypeStack.Count > 0)
			{
				activeOwnerTypeStack.Pop();
			}
		}
	}

	// 反序列化字段字典为可持久化对象。
	internal static object deserializePersistableObject(Dictionary<string, object> node, Type modelType)
	{
		activeOwnerTypeStack?.Push(modelType);
		object instance = Activator.CreateInstance(modelType, true)
			?? throw new InvalidOperationException($"类型 {modelType.FullName} 需要可用的无参构造函数");

		try
		{
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
		finally
		{
			if (activeOwnerTypeStack != null && activeOwnerTypeStack.Count > 0)
			{
				activeOwnerTypeStack.Pop();
			}
		}
	}
}
