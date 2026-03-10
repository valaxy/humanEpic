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

		if (trySerializeAtomicValue(value, actualType, out object atomicSerialized))
		{
			return atomicSerialized;
		}

		if (actualType.IsEnum)
		{
			return Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		if (isValueTupleType(actualType))
		{
			Type[] tupleElementTypes = getValueTupleElementTypes(actualType);
			List<object> tupleValues = tupleElementTypes
				.Select((elementType, index) =>
				{
					string fieldName = $"Item{index + 1}";
					object tupleElement = actualType.GetField(fieldName)?.GetValue(value)
						?? throw new InvalidOperationException($"值元组字段读取失败: {actualType.FullName}.{fieldName}");
					return serializeValue(tupleElement, elementType);
				})
				.ToList();

			return new Dictionary<string, object>
			{
				{ tupleTag, true },
				{ tupleItems, tupleValues }
			};
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

		if (tryGetSetElementType(actualType, out Type setElementType))
		{
			if (value is not IEnumerable enumerable)
			{
				throw new InvalidOperationException($"类型 {actualType.FullName} 不是可枚举集合");
			}

			List<object> items = enumerable
				.Cast<object>()
				.Select(item => serializeValue(item, setElementType))
				.ToList();

			return new Dictionary<string, object>
			{
				{ setTag, true },
				{ setItems, items }
			};
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
		if (tryDeserializeAtomicValue(rawValue, targetType, out object atomicDeserialized))
		{
			return atomicDeserialized;
		}

		if (targetType.IsEnum)
		{
			int enumId = Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
			return Enum.ToObject(targetType, enumId);
		}

		if (isValueTupleType(targetType))
		{
			if (rawValue is not Dictionary<string, object> tupleNode)
			{
				throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
			}

			if (!tupleNode.ContainsKey(tupleTag) || !tupleNode.ContainsKey(tupleItems))
			{
				throw new InvalidOperationException($"值元组数据结构非法: {targetType.FullName}");
			}

			if (tupleNode[tupleItems] is not IList tupleItemsRaw)
			{
				throw new InvalidOperationException($"值元组项结构非法: {targetType.FullName}");
			}

			Type[] tupleElementTypes = getValueTupleElementTypes(targetType);
			if (tupleItemsRaw.Count != tupleElementTypes.Length)
			{
				throw new InvalidOperationException($"值元组元素数量不匹配: {targetType.FullName}");
			}

			object[] tupleValues = tupleElementTypes
				.Select((elementType, index) =>
				{
					object rawTupleElement = tupleItemsRaw[index]
						?? throw new InvalidOperationException($"值元组元素不能为空: {targetType.FullName}.Item{index + 1}");
					return deserializeValue(rawTupleElement, elementType);
				})
				.ToArray();

			object? tupleValue = Activator.CreateInstance(targetType, tupleValues);
			return tupleValue
				?? throw new InvalidOperationException($"值元组实例化失败: {targetType.FullName}");
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

		if (tryGetSetElementType(targetType, out Type setElementType))
		{
			if (rawValue is not Dictionary<string, object> setNode)
			{
				throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
			}

			if (!setNode.ContainsKey(setTag) || !setNode.ContainsKey(setItems))
			{
				throw new InvalidOperationException($"集合数据结构非法: {targetType.FullName}");
			}

			if (setNode[setItems] is not IList rawSetItems)
			{
				throw new InvalidOperationException($"集合项结构非法: {targetType.FullName}");
			}

			object set = createSet(targetType, setElementType);
			Type setType = typeof(ISet<>).MakeGenericType(setElementType);
			System.Reflection.MethodInfo addMethod = setType.GetMethod("Add")
				?? throw new InvalidOperationException($"集合缺少 Add 方法: {targetType.FullName}");

			rawSetItems
				.Cast<object>()
				.Select(item => deserializeValue(item, setElementType))
				.ToList()
				.ForEach(item => addMethod.Invoke(set, new[] { item }));

			return set;
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
