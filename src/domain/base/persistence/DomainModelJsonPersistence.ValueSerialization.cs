using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	// 按声明类型递归序列化值。
	private static object serializeValue(object? value, Type declaredType)
	{
		if (value == null)
		{
			throw new InvalidOperationException($"类型 {declaredType.FullName} 的值为 null，无法持久化");
		}

		Type actualType = value.GetType();

		if (isBasicType(actualType))
		{
			return value;
		}

		if (actualType.IsEnum)
		{
			return Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		if (tryGetListElementType(actualType, out Type listElementType))
		{
			if (value is not IEnumerable enumerable)
			{
				throw new InvalidOperationException($"类型 {actualType.FullName} 不是可枚举集合");
			}

			return enumerable.Cast<object>()
				.Select(item => serializeValue(item, listElementType))
				.ToList();
		}

		if (tryGetDictionaryTypes(actualType, out Type keyType, out Type valueType))
		{
			if (value is not IDictionary dictionary)
			{
				throw new InvalidOperationException($"类型 {actualType.FullName} 不是字典集合");
			}

			if (!isSupportedDictionaryKeyType(keyType))
			{
				throw new InvalidOperationException($"字典键类型不支持持久化: {keyType.FullName}");
			}

			List<object> entries = dictionary.Keys
				.Cast<object>()
				.Select(key =>
				{
					object rawValue = dictionary[key]
						?? throw new InvalidOperationException($"字典值不能为空: {actualType.FullName}");
					return (object)new Dictionary<string, object>
					{
						{ entryKey, serializeValue(key, keyType) },
						{ entryValue, serializeValue(rawValue, valueType) }
					};
				})
				.ToList();

			return new Dictionary<string, object>
			{
				{ dictTag, true },
				{ dictEntries, entries }
			};
		}

		ensurePersistableClass(actualType);
		return serializePersistableObject(value, actualType);
	}

	// 按目标类型递归反序列化值。
	private static object deserializeValue(object rawValue, Type targetType)
	{
		if (isBasicType(targetType))
		{
			return convertBasic(rawValue, targetType);
		}

		if (targetType.IsEnum)
		{
			int enumId = Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
			return Enum.ToObject(targetType, enumId);
		}

		if (tryGetListElementType(targetType, out Type listElementType))
		{
			if (rawValue is not IList listRaw)
			{
				throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是数组");
			}

			IList list = createList(targetType, listElementType);
			listRaw
				.Cast<object>()
				.Select(item => deserializeValue(item, listElementType))
				.ToList()
				.ForEach(item => list.Add(item));
			return list;
		}

		if (tryGetDictionaryTypes(targetType, out Type keyType, out Type valueType))
		{
			if (rawValue is not Dictionary<string, object> dictNode)
			{
				throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
			}

			if (!dictNode.ContainsKey(dictTag) || !dictNode.ContainsKey(dictEntries))
			{
				throw new InvalidOperationException($"字典数据结构非法: {targetType.FullName}");
			}

			if (!isSupportedDictionaryKeyType(keyType))
			{
				throw new InvalidOperationException($"字典键类型不支持持久化: {keyType.FullName}");
			}

			if (dictNode[dictEntries] is not IList entriesRaw)
			{
				throw new InvalidOperationException($"字典条目结构非法: {targetType.FullName}");
			}

			IDictionary dictionary = createDictionary(targetType, keyType, valueType);
			entriesRaw
				.Cast<object>()
				.Select(item => item as Dictionary<string, object> ?? throw new InvalidOperationException($"字典条目不是对象: {targetType.FullName}"))
				.ToList()
				.ForEach(item =>
				{
					if (!item.ContainsKey(entryKey) || !item.ContainsKey(entryValue))
					{
						throw new InvalidOperationException($"字典条目缺少键值: {targetType.FullName}");
					}

					object key = deserializeValue(item[entryKey], keyType);
					object value = deserializeValue(item[entryValue], valueType);
					dictionary.Add(key, value);
				});
			return dictionary;
		}

		ensurePersistableClass(targetType);
		if (rawValue is not Dictionary<string, object> objectNode)
		{
			throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
		}

		return deserializePersistableObject(objectNode, targetType);
	}
}
