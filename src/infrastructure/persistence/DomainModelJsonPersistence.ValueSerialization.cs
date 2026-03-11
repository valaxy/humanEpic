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
		Type actualType = value.GetType();
		ITypePersistence? atomicPersistence = getAtomicTypePersistenceOrNull(targetType);
		if (atomicPersistence != null)
		{
			return atomicPersistence.Serialize(value, targetType);
		}

		ensurePersistableClass(targetType);
		return serializePersistableObject(value, targetType);
	}

	// 按目标类型递归反序列化值。
	internal static object deserializeValue(object rawValue, Type targetType)
	{
		if (rawValue == null)
		{
			bool nullableValueType = Nullable.GetUnderlyingType(targetType) != null;
			bool nullableReferenceType = !targetType.IsValueType;
			if (nullableValueType || nullableReferenceType)
			{
				return null!;
			}

			throw new InvalidOperationException($"类型 {targetType.FullName} 不可为 null");
		}

		Type effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
		ITypePersistence? atomicPersistence = getAtomicTypePersistenceOrNull(effectiveTargetType);
		if (atomicPersistence != null)
		{
			return atomicPersistence.Deserialize(rawValue, effectiveTargetType);
		}

		ensurePersistableClass(effectiveTargetType);
		if (rawValue is not Dictionary<string, object> objectNode)
		{
			throw new InvalidOperationException($"类型 {effectiveTargetType.FullName} 的数据不是对象");
		}

		return deserializePersistableObject(objectNode, effectiveTargetType);
	}
}
