using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

internal sealed class DictionaryTypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => DomainModelJsonPersistence.tryGetDictionaryTypes(type, out _, out _);

	public object Serialize(object value, Type declaredType)
	{
		if (!DomainModelJsonPersistence.tryGetDictionaryTypes(declaredType, out Type keyType, out Type valueType))
		{
			throw new InvalidOperationException($"类型不是字典: {declaredType.FullName}");
		}

		if (value is not IDictionary dictionary)
		{
			throw new InvalidOperationException($"类型 {declaredType.FullName} 不是字典集合");
		}

		if (!DomainModelJsonPersistence.isSupportedDictionaryKeyType(keyType))
		{
			throw new InvalidOperationException($"字典键类型不支持持久化: {keyType.FullName}");
		}

		List<object> entries = dictionary.Keys
			.Cast<object>()
			.Select(key =>
			{
				object rawValue = dictionary[key]
					?? throw new InvalidOperationException($"字典值不能为空: {declaredType.FullName}");
				return (object)new Dictionary<string, object>
				{
					{ "k", DomainModelJsonPersistence.serializeValue(key, keyType) },
					{ "v", DomainModelJsonPersistence.serializeValue(rawValue, valueType) }
				};
			})
			.ToList();

		return new Dictionary<string, object>
		{
			{ "__dict", true },
			{ "entries", entries }
		};
	}

	public object Deserialize(object rawValue, Type targetType)
	{
		if (!DomainModelJsonPersistence.tryGetDictionaryTypes(targetType, out Type keyType, out Type valueType))
		{
			throw new InvalidOperationException($"类型不是字典: {targetType.FullName}");
		}

		if (rawValue is not Dictionary<string, object> dictNode)
		{
			throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
		}

		if (!dictNode.ContainsKey("__dict") || !dictNode.ContainsKey("entries"))
		{
			throw new InvalidOperationException($"字典数据结构非法: {targetType.FullName}");
		}

		if (!DomainModelJsonPersistence.isSupportedDictionaryKeyType(keyType))
		{
			throw new InvalidOperationException($"字典键类型不支持持久化: {keyType.FullName}");
		}

		if (dictNode["entries"] is not IList entriesRaw)
		{
			throw new InvalidOperationException($"字典条目结构非法: {targetType.FullName}");
		}

		IDictionary dictionary = DomainModelJsonPersistence.createDictionary(targetType, keyType, valueType);
		entriesRaw
			.Cast<object>()
			.Select(item => item as Dictionary<string, object> ?? throw new InvalidOperationException($"字典条目不是对象: {targetType.FullName}"))
			.ToList()
			.ForEach(item =>
			{
				if (!item.ContainsKey("k") || !item.ContainsKey("v"))
				{
					throw new InvalidOperationException($"字典条目缺少键值: {targetType.FullName}");
				}

				object key = DomainModelJsonPersistence.deserializeValue(item["k"], keyType);
				object value = DomainModelJsonPersistence.deserializeValue(item["v"], valueType);
				dictionary.Add(key, value);
			});
		return dictionary;
	}
}
