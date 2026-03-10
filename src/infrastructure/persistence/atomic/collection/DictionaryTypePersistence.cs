using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

internal sealed class DictionaryTypePersistence : ITypePersistence
{
	public bool CanHandle(Type type) => TypeHelpers.isDictionaryLikeType(type);

	public object Serialize(object value, Type declaredType)
	{
		(Type keyType, Type valueType) = TypeHelpers.getDictionaryTypes(declaredType);

		if (value is not IDictionary dictionary)
		{
			throw new InvalidOperationException($"类型 {declaredType.FullName} 不是字典集合");
		}

		if (!TypeHelpers.isSupportedDictionaryKeyType(keyType))
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
		(Type keyType, Type valueType) = TypeHelpers.getDictionaryTypes(targetType);

		if (rawValue is not Dictionary<string, object> dictNode)
		{
			throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
		}

		if (!dictNode.ContainsKey("__dict"))
		{
			throw new InvalidOperationException($"字典数据结构非法: {targetType.FullName}");
		}

		if (!TypeHelpers.isSupportedDictionaryKeyType(keyType))
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
		if (keyType == typeof(Vector2I))
		{
			Vector2I vectorKey = (Vector2I)key;
			return $"{vectorKey.X},{vectorKey.Y}";
		}

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

		if (keyType == typeof(Vector2I))
		{
			string[] parts = keyText.Split(',');
			if (parts.Length != 2)
			{
				throw new InvalidOperationException($"字典键 Vector2I 格式非法: {targetType.FullName}");
			}

			int x = int.Parse(parts[0], CultureInfo.InvariantCulture);
			int y = int.Parse(parts[1], CultureInfo.InvariantCulture);
			return new Vector2I(x, y);
		}

		return Convert.ChangeType(keyText, keyType, CultureInfo.InvariantCulture)
			?? throw new InvalidOperationException($"字典键转换失败: {targetType.FullName}");
	}
}
