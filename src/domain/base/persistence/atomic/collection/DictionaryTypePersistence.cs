using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

internal sealed class DictionaryTypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => DomainModelJsonPersistence.isDictionaryLikeType(type);

	public object Serialize(object value, Type declaredType)
	{
		(Type keyType, Type valueType) = DomainModelJsonPersistence.getDictionaryTypes(declaredType);

		if (value is not IDictionary dictionary)
		{
			throw new InvalidOperationException($"类型 {declaredType.FullName} 不是字典集合");
		}

		if (!DomainModelJsonPersistence.isSupportedDictionaryKeyType(keyType))
		{
			throw new InvalidOperationException($"字典键类型不支持持久化: {keyType.FullName}");
		}

		Dictionary<string, object> kv = dictionary.Keys
			.Cast<object>()
			.Select(key =>
			{
				object rawValue = dictionary[key]
					?? throw new InvalidOperationException($"字典值不能为空: {declaredType.FullName}");

				string keyText = serializeDictionaryKey(key, keyType);
				object valueNode = DomainModelJsonPersistence.serializeValue(rawValue, valueType);
				return (keyText, valueNode);
			})
			.ToDictionary(item => item.keyText, item => item.valueNode);

		return new Dictionary<string, object>
		{
			{ "__dict", true },
			{ "kv", kv }
		};
	}

	public object Deserialize(object rawValue, Type targetType)
	{
		(Type keyType, Type valueType) = DomainModelJsonPersistence.getDictionaryTypes(targetType);

		if (rawValue is not Dictionary<string, object> dictNode)
		{
			throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
		}

		if (!dictNode.ContainsKey("__dict"))
		{
			throw new InvalidOperationException($"字典数据结构非法: {targetType.FullName}");
		}

		if (!DomainModelJsonPersistence.isSupportedDictionaryKeyType(keyType))
		{
			throw new InvalidOperationException($"字典键类型不支持持久化: {keyType.FullName}");
		}

		IDictionary dictionary = DomainModelJsonPersistence.createDictionary(targetType, keyType, valueType);

		if (dictNode.TryGetValue("kv", out object? kvRaw))
		{
			if (kvRaw is not Dictionary<string, object> kv)
			{
				throw new InvalidOperationException($"字典 kv 结构非法: {targetType.FullName}");
			}

			kv.ToList().ForEach(item =>
			{
				object key = deserializeDictionaryKey(item.Key, keyType, targetType);
				object value = DomainModelJsonPersistence.deserializeValue(item.Value, valueType);
				dictionary.Add(key, value);
			});
			return dictionary;
		}

		throw new InvalidOperationException($"字典 kv 结构缺失: {targetType.FullName}");
	}

	private static string serializeDictionaryKey(object key, Type keyType)
	{
		object keyNode = DomainModelJsonPersistence.serializeValue(key, keyType);
		return Convert.ToString(keyNode, CultureInfo.InvariantCulture)
			?? throw new InvalidOperationException($"字典键无法转换为字符串: {keyType.FullName}");
	}

	private static object deserializeDictionaryKey(string keyText, Type keyType, Type targetType)
	{
		if (keyType == typeof(string))
		{
			return keyText;
		}

		if (keyType.IsEnum)
		{
			int enumId = int.Parse(keyText, CultureInfo.InvariantCulture);
			return Enum.ToObject(keyType, enumId);
		}

		return Convert.ChangeType(keyText, keyType, CultureInfo.InvariantCulture)
			?? throw new InvalidOperationException($"字典键转换失败: {targetType.FullName}");
	}
}
