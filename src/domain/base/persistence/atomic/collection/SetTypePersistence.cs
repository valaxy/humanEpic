using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

internal sealed class SetTypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => DomainModelJsonPersistence.tryGetSetElementType(type, out _);

	public object Serialize(object value, Type declaredType)
	{
		if (!DomainModelJsonPersistence.tryGetSetElementType(declaredType, out Type elementType))
		{
			throw new InvalidOperationException($"类型不是集合: {declaredType.FullName}");
		}

		if (value is not IEnumerable enumerable)
		{
			throw new InvalidOperationException($"类型 {declaredType.FullName} 不是可枚举集合");
		}

		List<object> items = enumerable
			.Cast<object>()
			.Select(item => DomainModelJsonPersistence.serializeValue(item, elementType))
			.ToList();

		return new Dictionary<string, object>
		{
			{ "__set", true },
			{ "items", items }
		};
	}

	public object Deserialize(object rawValue, Type targetType)
	{
		if (!DomainModelJsonPersistence.tryGetSetElementType(targetType, out Type elementType))
		{
			throw new InvalidOperationException($"类型不是集合: {targetType.FullName}");
		}

		if (rawValue is not Dictionary<string, object> setNode)
		{
			throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
		}

		if (!setNode.ContainsKey("__set") || !setNode.ContainsKey("items"))
		{
			throw new InvalidOperationException($"集合数据结构非法: {targetType.FullName}");
		}

		if (setNode["items"] is not IList rawSetItems)
		{
			throw new InvalidOperationException($"集合项结构非法: {targetType.FullName}");
		}

		object set = DomainModelJsonPersistence.createSet(targetType, elementType);
		Type setType = typeof(ISet<>).MakeGenericType(elementType);
		MethodInfo addMethod = setType.GetMethod("Add")
			?? throw new InvalidOperationException($"集合缺少 Add 方法: {targetType.FullName}");

		rawSetItems
			.Cast<object>()
			.Select(item => DomainModelJsonPersistence.deserializeValue(item, elementType))
			.ToList()
			.ForEach(item => addMethod.Invoke(set, new[] { item }));

		return set;
	}
}
