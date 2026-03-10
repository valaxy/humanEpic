using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	// 基础类型集合。
	private static bool isBasicType(Type type)
	{
		return type == typeof(string)
			|| type == typeof(bool)
			|| type == typeof(byte)
			|| type == typeof(sbyte)
			|| type == typeof(short)
			|| type == typeof(ushort)
			|| type == typeof(int)
			|| type == typeof(uint)
			|| type == typeof(long)
			|| type == typeof(ulong)
			|| type == typeof(float)
			|| type == typeof(double)
			|| type == typeof(decimal);
	}

	// 判断是否为值元组类型。
	private static bool isValueTupleType(Type type)
	{
		if (!type.IsValueType || !type.IsGenericType)
		{
			return false;
		}

		Type genericType = type.GetGenericTypeDefinition();
		return genericType == typeof(ValueTuple<>)
			|| genericType == typeof(ValueTuple<,>)
			|| genericType == typeof(ValueTuple<,,>)
			|| genericType == typeof(ValueTuple<,,,>)
			|| genericType == typeof(ValueTuple<,,,,>)
			|| genericType == typeof(ValueTuple<,,,,,>)
			|| genericType == typeof(ValueTuple<,,,,,,>)
			|| genericType == typeof(ValueTuple<,,,,,,,>);
	}

	// 获取值元组元素类型。
	private static Type[] getValueTupleElementTypes(Type tupleType)
	{
		if (!isValueTupleType(tupleType))
		{
			throw new InvalidOperationException($"类型不是值元组: {tupleType.FullName}");
		}

		return tupleType.GetGenericArguments();
	}

	// 校验类型是否有可持久化标记。
	private static void ensurePersistableClass(Type type)
	{
		if (type.GetCustomAttribute<PersistableAttribute>() == null)
		{
			throw new InvalidOperationException($"类型未标记 [Persistable]: {type.FullName}");
		}
	}

	// 获取被持久化标记的字段集合。
	private static List<(FieldInfo field, PersistFieldAttribute attr)> getPersistFields(Type type)
	{
		return enumerateTypeChain(type)
			.SelectMany(currentType => currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			.Select(field => (field, attr: field.GetCustomAttribute<PersistFieldAttribute>(true)))
			.Where(item => item.attr != null)
			.Select(item => (item.field, item.attr!))
			.ToList();
	}

	// 获取被持久化标记的静态字段集合。
	private static List<(FieldInfo field, PersistFieldAttribute attr)> getPersistStaticFields(Type type)
	{
		return enumerateTypeChain(type)
			.SelectMany(currentType => currentType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			.Select(field => (field, attr: field.GetCustomAttribute<PersistFieldAttribute>(true)))
			.Where(item => item.attr != null)
			.Select(item => (item.field, item.attr!))
			.ToList();
	}

	// 获取被持久化标记的属性集合。
	private static List<(PropertyInfo property, PersistPropertyAttribute attr)> getPersistProperties(Type type)
	{
		return enumerateTypeChain(type)
			.SelectMany(currentType => currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			.Select(property => (property, attr: property.GetCustomAttribute<PersistPropertyAttribute>(true)))
			.Where(item => item.attr != null)
			.Select(item => (item.property, item.attr!))
			.ToList();
	}

	// 获取被持久化标记的静态属性集合。
	private static List<(PropertyInfo property, PersistPropertyAttribute attr)> getPersistStaticProperties(Type type)
	{
		return enumerateTypeChain(type)
			.SelectMany(currentType => currentType.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			.Select(property => (property, attr: property.GetCustomAttribute<PersistPropertyAttribute>(true)))
			.Where(item => item.attr != null)
			.Select(item => (item.property, item.attr!))
			.ToList();
	}

	// 枚举从派生到基类（不含 object）的类型链。
	private static IEnumerable<Type> enumerateTypeChain(Type type)
	{
		Type? current = type;
		while (current != null && current != typeof(object))
		{
			yield return current;
			current = current.BaseType;
		}
	}

	// 尝试获取列表元素类型。
	private static bool tryGetListElementType(Type type, out Type elementType)
	{
		if (type.IsArray)
		{
			elementType = type.GetElementType()
				?? throw new InvalidOperationException($"数组元素类型不可用: {type.FullName}");
			return true;
		}

		Type? listInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
			? type
			: type.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

		if (listInterface == null)
		{
			elementType = typeof(object);
			return false;
		}

		elementType = listInterface.GetGenericArguments()[0];
		return true;
	}

	// 尝试获取集合元素类型（HashSet/ISet）。
	private static bool tryGetSetElementType(Type type, out Type elementType)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
		{
			elementType = type.GetGenericArguments()[0];
			return true;
		}

		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SortedSet<>))
		{
			elementType = type.GetGenericArguments()[0];
			return true;
		}

		Type? setInterface = type.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISet<>));

		if (setInterface == null)
		{
			elementType = typeof(object);
			return false;
		}

		elementType = setInterface.GetGenericArguments()[0];
		return true;
	}

	// 尝试获取字典键值类型。
	private static bool tryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
	{
		Type? dictInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)
			? type
			: type.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

		if (dictInterface == null)
		{
			keyType = typeof(object);
			valueType = typeof(object);
			return false;
		}

		Type[] args = dictInterface.GetGenericArguments();
		keyType = args[0];
		valueType = args[1];
		return true;
	}

	// 字典键支持基础类型和枚举。
	private static bool isSupportedDictionaryKeyType(Type type)
	{
		return isBasicType(type) || type.IsEnum;
	}

	// 创建列表实例。
	private static IList createList(Type declaredListType, Type elementType)
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
	private static IDictionary createDictionary(Type declaredDictType, Type keyType, Type valueType)
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
	private static object createSet(Type declaredSetType, Type elementType)
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
}
