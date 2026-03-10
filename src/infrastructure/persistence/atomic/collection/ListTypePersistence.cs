using System;
using System.Collections;
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

		IList list = DomainModelJsonPersistence.createList(targetType, elementType);
		listRaw
			.Cast<object>()
			.Select(item => DomainModelJsonPersistence.deserializeValue(item, elementType))
			.ToList()
			.ForEach(item => list.Add(item));
		return list;
	}
}
