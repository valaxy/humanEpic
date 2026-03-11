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
	[ThreadStatic]
	private static Stack<Type>? activeOwnerTypeStack;

	[ThreadStatic]
	private static Dictionary<Type, object>? activeEntityCollections;

	[ThreadStatic]
	private static bool activeIncludeStaticSidecar;

	/// <summary>
	/// 一次 Save/Load 根流程的线程局部上下文作用域。
	/// 作用：
	/// 1) 维护当前“宿主类型”栈，支持实体引用在“集合内全量序列化 / 集合外按 ID 引用”之间切换；
	/// 2) 维护实体集合映射，用于反序列化时按 ID 回查实体；
	/// 3) 控制本次流程是否处理根节点 __static sidecar。
	/// </summary>
	private sealed class PersistenceScope : IDisposable
	{
		private readonly Stack<Type>? previousOwnerTypeStack;
		private readonly Dictionary<Type, object>? previousEntityCollections;
		private readonly bool previousIncludeStaticSidecar;

		public PersistenceScope(Type ownerType, IEnumerable<object> entityCollections, bool includeStaticSidecar)
		{
			previousOwnerTypeStack = activeOwnerTypeStack;
			previousEntityCollections = activeEntityCollections;
			previousIncludeStaticSidecar = activeIncludeStaticSidecar;

			activeOwnerTypeStack = new Stack<Type>();
			activeOwnerTypeStack.Push(ownerType);
			activeEntityCollections = createEntityCollectionMap(entityCollections);
			activeIncludeStaticSidecar = includeStaticSidecar;
		}

		public void Dispose()
		{
			activeOwnerTypeStack = previousOwnerTypeStack;
			activeEntityCollections = previousEntityCollections;
			activeIncludeStaticSidecar = previousIncludeStaticSidecar;
		}
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
			Dictionary<string, object> staticNode = serializeStaticMembers(PersistenceReflectionHelper.getAllPersistableTypesInCurrentAssembly());
			instanceNode[staticMembers] = staticNode;
		}

		return instanceNode;
	}


	private static Dictionary<string, object> serializeStaticMembersForType(Type modelType)
	{
		List<(FieldInfo field, PersistFieldAttribute attr)> staticFields = PersistenceReflectionHelper.getPersistStaticFields(modelType);
		Dictionary<string, object> staticFieldMap = staticFields.ToDictionary(
			item => string.IsNullOrWhiteSpace(item.attr.Name) ? item.field.Name : item.attr.Name,
			item => serializeValue(item.field.GetValue(null), item.field.FieldType));

		List<(PropertyInfo property, PersistPropertyAttribute attr)> staticProperties = PersistenceReflectionHelper.getPersistStaticProperties(modelType);
		Dictionary<string, object> staticPropertyMap = staticProperties.ToDictionary(
			item => string.IsNullOrWhiteSpace(item.attr.Name) ? item.property.Name : item.attr.Name,
			item =>
			{
				Debug.Assert(item.property.GetMethod != null, $"静态属性缺少 getter，无法序列化: {modelType.FullName}.{item.property.Name}");

				return serializeValue(item.property.GetValue(null), item.property.PropertyType);
			});

		Debug.Assert(!staticFieldMap.Keys.Intersect(staticPropertyMap.Keys).Any(), $"类型存在重复静态持久化键: {modelType.FullName}");

		return staticFieldMap.Concat(staticPropertyMap)
			.ToDictionary(item => item.Key, item => item.Value);
	}

	// 创建静态成员快照（根节点 sidecar）。
	private static Dictionary<string, object> serializeStaticMembers(IEnumerable<Type> types)
	{
		List<(string key, Type type, Dictionary<string, object> node)> typeEntries = types
			.Select(type => (type, node: serializeStaticMembersForType(type)))
			.Where(item => item.node.Count > 0)
			.Select(item => (key: item.type.Name, item.type, item.node))
			.ToList();

		Debug.Assert(!typeEntries
			.GroupBy(item => item.key)
			.Any(group => group.Count() > 1), "静态成员类型名冲突，无法使用简单类名作为键");

		return typeEntries.ToDictionary(item => item.key, item => (object)item.node);
	}





	// 将可持久化对象序列化为字段字典。
	internal static Dictionary<string, object> serializePersistableObject(object model, Type modelType)
	{
		activeOwnerTypeStack?.Push(modelType);

		try
		{
			List<(FieldInfo field, PersistFieldAttribute attr)> fields = PersistenceReflectionHelper.getPersistFields(modelType);
			Dictionary<string, object> fieldMap = fields.ToDictionary(
				item => string.IsNullOrWhiteSpace(item.attr.Name) ? item.field.Name : item.attr.Name,
				item => serializeValue(item.field.GetValue(model), item.field.FieldType)
			);

			List<(PropertyInfo property, PersistPropertyAttribute attr)> properties = PersistenceReflectionHelper.getPersistProperties(modelType);
			Dictionary<string, object> propertyMap = properties.ToDictionary(
				item => string.IsNullOrWhiteSpace(item.attr.Name) ? item.property.Name : item.attr.Name,
				item =>
				{
					Debug.Assert(item.property.GetMethod != null, $"属性缺少 getter，无法序列化: {modelType.FullName}.{item.property.Name}");

					return serializeValue(item.property.GetValue(model), item.property.PropertyType);
				}
			);

			Debug.Assert(!fieldMap.Keys.Intersect(propertyMap.Keys).Any(), $"类型存在重复持久化键: {modelType.FullName}");

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

	// 按声明类型递归序列化值。
	internal static object serializeValue(object? value, Type declaredType)
	{
		if (value == null)
		{
			bool nullableValueType = Nullable.GetUnderlyingType(declaredType) != null;
			bool nullableReferenceType = !declaredType.IsValueType;
			if (nullableValueType || nullableReferenceType)
			{
				return null!;
			}

			throw new InvalidOperationException($"类型 {declaredType.FullName} 的值为 null，无法持久化");
		}

		Type? nullableUnderlyingType = Nullable.GetUnderlyingType(declaredType);
		Type targetType = nullableUnderlyingType ?? value.GetType();
		ITypePersistence typePersistence = TypePeristenceFactory.GetTypePersistence(targetType);
		return typePersistence.Serialize(value, targetType);
	}
}
