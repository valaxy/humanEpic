using System;
using System.Collections.Generic;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	// 按声明类型递归序列化值。
	internal static object serializeValue(object? value, Type declaredType)
	{
		if (value == null)
		{
			throw new InvalidOperationException($"类型 {declaredType.FullName} 的值为 null，无法持久化");
		}

		Type actualType = value.GetType();
		IAtomicTypePersistence? atomicPersistence = getAtomicTypePersistenceOrNull(actualType);
		if (atomicPersistence != null)
		{
			return atomicPersistence.Serialize(value, actualType);
		}

		ensurePersistableClass(actualType);
		return serializePersistableObject(value, actualType);
	}

	// 按目标类型递归反序列化值。
	internal static object deserializeValue(object rawValue, Type targetType)
	{
		IAtomicTypePersistence? atomicPersistence = getAtomicTypePersistenceOrNull(targetType);
		if (atomicPersistence != null)
		{
			return atomicPersistence.Deserialize(rawValue, targetType);
		}

		ensurePersistableClass(targetType);
		if (rawValue is not Dictionary<string, object> objectNode)
		{
			throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
		}

		return deserializePersistableObject(objectNode, targetType);
	}
}
