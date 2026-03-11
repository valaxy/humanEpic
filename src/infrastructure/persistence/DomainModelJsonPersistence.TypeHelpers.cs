using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;
using Godot;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	// 获取值元组元素类型。
	internal static Type[] getValueTupleElementTypes(Type tupleType)
	{
		Debug.Assert(TypeHelpers.isValueTupleType(tupleType), $"类型不是值元组: {tupleType.FullName}");
		return tupleType.GetGenericArguments();
	}



	internal static bool shouldSerializeEntityAsFullObject(Type entityType)
	{
		Type ownerType = getCurrentOwnerType();
		Type collectionType = PersistenceReflectionHelper.getEntityCollectionType(entityType);
		return collectionType.IsAssignableFrom(ownerType);
	}

	// 创建列表实例。
	internal static IList createList(Type declaredListType, Type elementType)
	{
		if (declaredListType.IsArray)
		{
			object? arrayList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
			return arrayList as IList
				?? throw new InvalidOperationException($"无法创建列表实例: {declaredListType.FullName}");
		}

		if (declaredListType.IsInterface || declaredListType.IsAbstract)
		{
			object? interfaceList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
			return interfaceList as IList
				?? throw new InvalidOperationException($"无法创建列表实例: {declaredListType.FullName}");
		}

		object? list = Activator.CreateInstance(declaredListType);
		return list as IList
			?? throw new InvalidOperationException($"无法创建列表实例: {declaredListType.FullName}");
	}

	// 创建字典实例。
	internal static IDictionary createDictionary(Type declaredDictType, Type keyType, Type valueType)
	{
		if (declaredDictType.IsInterface || declaredDictType.IsAbstract)
		{
			object? interfaceDict = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
			return interfaceDict as IDictionary
				?? throw new InvalidOperationException($"无法创建字典实例: {declaredDictType.FullName}");
		}

		object? dictionary = Activator.CreateInstance(declaredDictType);
		return dictionary as IDictionary
			?? throw new InvalidOperationException($"无法创建字典实例: {declaredDictType.FullName}");
	}

	// 创建集合实例。
	internal static object createSet(Type declaredSetType, Type elementType)
	{
		if (declaredSetType.IsGenericType && declaredSetType.GetGenericTypeDefinition() == typeof(SortedSet<>))
		{
			return createSortedSetWithFallbackComparer(elementType)
				?? throw new InvalidOperationException($"无法创建有序集合实例: {declaredSetType.FullName}");
		}

		if (declaredSetType.IsInterface || declaredSetType.IsAbstract)
		{
			return Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType))
				?? throw new InvalidOperationException($"无法创建集合实例: {declaredSetType.FullName}");
		}

		return Activator.CreateInstance(declaredSetType)
			?? throw new InvalidOperationException($"无法创建集合实例: {declaredSetType.FullName}");
	}

	private static object createSortedSetWithFallbackComparer(Type elementType)
	{
		MethodInfo method = typeof(DomainModelJsonPersistence)
			.GetMethod(nameof(createSortedSetWithFallbackComparerGeneric), BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("创建 SortedSet 失败：未找到工厂方法");
		MethodInfo genericMethod = method.MakeGenericMethod(elementType);
		return genericMethod.Invoke(null, null)
			?? throw new InvalidOperationException($"无法创建有序集合实例: {elementType.FullName}");
	}

	private static SortedSet<T> createSortedSetWithFallbackComparerGeneric<T>()
	{
		IComparer<T> comparer = typeof(IComparable<T>).IsAssignableFrom(typeof(T)) || typeof(IComparable).IsAssignableFrom(typeof(T))
			? Comparer<T>.Default
			: Comparer<T>.Create((left, right) =>
			{
				if (ReferenceEquals(left, right))
				{
					return 0;
				}

				if (left == null)
				{
					return -1;
				}

				if (right == null)
				{
					return 1;
				}

				int hashCompare = RuntimeHelpers.GetHashCode(left).CompareTo(RuntimeHelpers.GetHashCode(right));
				if (hashCompare != 0)
				{
					return hashCompare;
				}

				return string.CompareOrdinal(left.ToString(), right.ToString());
			});

		return new SortedSet<T>(comparer);
	}


	internal static Dictionary<string, object> readGodotNode(object rawValue, string targetTypeName)
	{
		if (rawValue is not Dictionary<string, object> node)
		{
			throw new InvalidOperationException($"Godot 值类型数据结构非法: {targetTypeName}");
		}

		if (!node.ContainsKey("__godot") || !node.ContainsKey("type"))
		{
			throw new InvalidOperationException($"Godot 值类型缺少必要键: {targetTypeName}");
		}

		if (node["type"].ToString() != targetTypeName)
		{
			throw new InvalidOperationException($"Godot 值类型不匹配: 期望 {targetTypeName}");
		}

		return node
			.Where(pair => pair.Key != "__godot" && pair.Key != "type")
			.ToDictionary(pair => pair.Key, pair => pair.Value);
	}
}
