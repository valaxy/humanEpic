using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// 实体集合解析器，负责集合实例注册与按 ID 查询。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	private static Dictionary<Type, object> createEntityCollectionMap(IEnumerable<object> entityCollections)
	{
		return entityCollections
			.Where(collection => collection != null)
			.ToDictionary(collection => collection.GetType(), collection => collection);
	}

	private static bool tryGetEntityCollectionByType(Type collectionType, out object collection)
	{
		if (activeEntityCollections != null && activeEntityCollections.TryGetValue(collectionType, out object? found))
		{
			collection = found;
			return true;
		}

		collection = null!;
		return false;
	}

	internal static object resolveEntityById(Type entityType, int entityId)
	{
		Type collectionType = getEntityCollectionType(entityType);
		if (!tryGetEntityCollectionByType(collectionType, out object collection))
		{
			throw new InvalidOperationException($"实体类型 {entityType.FullName} 反持久化缺少集合上下文: {collectionType.FullName}");
		}

		MethodInfo? getByIdMethod = collectionType.GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public);
		if (getByIdMethod != null)
		{
			object? entity = getByIdMethod.Invoke(collection, new object[] { entityId });
			return entity ?? throw new InvalidOperationException($"实体查询结果为空: {entityType.FullName}#{entityId}");
		}

		MethodInfo? getMethod = collectionType.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(int) }, null);
		if (getMethod == null)
		{
			throw new InvalidOperationException($"集合类型缺少 Get(int)/GetById(int): {collectionType.FullName}");
		}

		object? resolved = getMethod.Invoke(collection, new object[] { entityId });
		return resolved ?? throw new InvalidOperationException($"实体查询结果为空: {entityType.FullName}#{entityId}");
	}

	private static Type getCurrentOwnerType()
	{
		if (activeOwnerTypeStack == null || activeOwnerTypeStack.Count == 0)
		{
			throw new InvalidOperationException("当前无持久化宿主类型上下文");
		}

		return activeOwnerTypeStack.Peek();
	}
}
