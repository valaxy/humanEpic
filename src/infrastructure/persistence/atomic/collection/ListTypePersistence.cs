using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

internal sealed class ListTypePersistence : ITypePersistence
{
	public bool CanHandle(Type type) => TypeHelpers.isListLikeType(type);

	public object Serialize(object value, Type declaredType)
	{
		Type elementType = TypeHelpers.getListElementType(declaredType);

		if (value is not IEnumerable enumerable)
		{
			throw new InvalidOperationException($"类型 {declaredType.FullName} 不是可枚举集合");
		}

		return enumerable.Cast<object>()
			.Select(item => DomainModelJsonPersistence.serializeValue(item, elementType))
			.ToList();
	}

	public object Deserialize(object rawValue, Type targetType)
	{
		Type elementType = TypeHelpers.getListElementType(targetType);

		if (rawValue is not IList listRaw)
		{
			throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是数组");
		}

		IList list = createList(targetType, elementType);
		listRaw
			.Cast<object>()
			.Select(item => DomainModelJsonPersistence.deserializeValue(item, elementType))
			.ToList()
			.ForEach(item => list.Add(item));
		return list;
	}


	// 创建列表实例。
	private IList createList(Type declaredListType, Type elementType)
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
}
