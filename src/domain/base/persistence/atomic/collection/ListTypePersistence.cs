using System;
using System.Collections;
using System.Linq;

internal sealed class ListTypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => DomainModelJsonPersistence.tryGetListElementType(type, out _);

	public object Serialize(object value, Type declaredType)
	{
		if (!DomainModelJsonPersistence.tryGetListElementType(declaredType, out Type elementType))
		{
			throw new InvalidOperationException($"类型不是列表: {declaredType.FullName}");
		}

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
		if (!DomainModelJsonPersistence.tryGetListElementType(targetType, out Type elementType))
		{
			throw new InvalidOperationException($"类型不是列表: {targetType.FullName}");
		}

		if (rawValue is not IList listRaw)
		{
			throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是数组");
		}

		IList list = DomainModelJsonPersistence.createList(targetType, elementType);
		listRaw
			.Cast<object>()
			.Select(item => DomainModelJsonPersistence.deserializeValue(item, elementType))
			.ToList()
			.ForEach(item => list.Add(item));
		return list;
	}
}
