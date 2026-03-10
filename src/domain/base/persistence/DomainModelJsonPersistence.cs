using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static class DomainModelJsonPersistence
{
	// 字典元数据键：标记当前对象节点是字典包装结构。
	private const string dictTag = "__dict";
	// 字典元数据键：字典条目列表键名。
	private const string dictEntries = "entries";
	// 字典元数据键：条目 key 键名。
	private const string entryKey = "k";
	// 字典元数据键：条目 value 键名。
	private const string entryValue = "v";


	/// <summary>
	/// 将模型保存为 JSON 字符串。
	/// </summary>
	public static string Save<TModel>(TModel model) where TModel : class
	{
		Dictionary<string, object> data = SaveToObject(model);
		return JsonSerializer.Serialize(data);
	}

	/// <summary>
	/// 将 JSON 字符串加载为模型。
	/// </summary>
	public static TModel Load<TModel>(string json) where TModel : class
	{
		using JsonDocument document = JsonDocument.Parse(json);
		object node = toNativeObject(document.RootElement);
		if (node is not Dictionary<string, object> root)
		{
			throw new InvalidOperationException("根节点必须是对象");
		}

		return LoadFromObject<TModel>(root);
	}

	/// <summary>
	/// 将模型保存为中间字典对象。
	/// </summary>
	public static Dictionary<string, object> SaveToObject<TModel>(TModel model) where TModel : class
	{
		if (model == null)
		{
			throw new InvalidOperationException("持久化模型不能为空");
		}

		Type modelType = model.GetType();
		ensurePersistableClass(modelType);
		return serializePersistableObject(model, modelType);
	}

	/// <summary>
	/// 从中间字典对象加载模型。
	/// </summary>
	public static TModel LoadFromObject<TModel>(Dictionary<string, object> data) where TModel : class
	{
		Type modelType = typeof(TModel);
		ensurePersistableClass(modelType);
		return (TModel)deserializePersistableObject(data, modelType);
	}


	// 将可持久化对象序列化为字段字典。
	private static Dictionary<string, object> serializePersistableObject(object model, Type modelType)
	{
		List<(FieldInfo field, PersistFieldAttribute attr)> fields = getPersistFields(modelType);
		return fields.ToDictionary(
			item => string.IsNullOrWhiteSpace(item.attr.Name) ? item.field.Name : item.attr.Name,
			item => serializeValue(item.field.GetValue(model), item.field.FieldType)
		);
	}

	// 反序列化字段字典为可持久化对象。
	private static object deserializePersistableObject(Dictionary<string, object> node, Type modelType)
	{
		object instance = Activator.CreateInstance(modelType)
			?? throw new InvalidOperationException($"类型 {modelType.FullName} 需要可用的无参构造函数");

		List<(FieldInfo field, PersistFieldAttribute attr)> fields = getPersistFields(modelType);
		fields
			.ToList()
			.ForEach(item =>
			{
				string key = string.IsNullOrWhiteSpace(item.attr.Name) ? item.field.Name : item.attr.Name;
				if (!node.ContainsKey(key))
				{
					throw new InvalidOperationException($"反持久化缺少字段: {modelType.FullName}.{item.field.Name}");
				}

				object value = deserializeValue(node[key], item.field.FieldType);
				item.field.SetValue(instance, value);
			});

		return instance;
	}

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

	// 基础类型集合。
	private static bool isBasicType(Type type)
	{
		return type == typeof(string)
			|| type == typeof(bool)
			|| type == typeof(byte)
			|| type == typeof(sbyte)
			|| type == typeof(short)
			|| type == typeof(ushort)
			|| type == typeof(int)
			|| type == typeof(uint)
			|| type == typeof(long)
			|| type == typeof(ulong)
			|| type == typeof(float)
			|| type == typeof(double)
			|| type == typeof(decimal);
	}

	// 校验类型是否有可持久化标记。
	private static void ensurePersistableClass(Type type)
	{
		if (type.GetCustomAttribute<PersistableAttribute>() == null)
		{
			throw new InvalidOperationException($"类型未标记 [Persistable]: {type.FullName}");
		}
	}

	// 获取被持久化标记的字段集合。
	private static List<(FieldInfo field, PersistFieldAttribute attr)> getPersistFields(Type type)
	{
		return type
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Select(field => (field, attr: field.GetCustomAttribute<PersistFieldAttribute>(true)))
			.Where(item => item.attr != null)
			.Select(item => (item.field, item.attr!))
			.ToList();
	}

	// 将 JSON 节点转换为原生对象图。
	private static object toNativeObject(JsonElement element)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				return element
					.EnumerateObject()
					.ToDictionary(prop => prop.Name, prop => toNativeObject(prop.Value));
			case JsonValueKind.Array:
				return element
					.EnumerateArray()
					.Select(toNativeObject)
					.Cast<object>()
					.ToList();
			case JsonValueKind.Number:
				return element.TryGetInt64(out long l)
					? (object)l
					: element.GetDouble();
			case JsonValueKind.String:
				return element.GetString()
					?? throw new InvalidOperationException("字符串值不能为空");
			case JsonValueKind.True:
				return true;
			case JsonValueKind.False:
				return false;
			default:
				throw new InvalidOperationException($"不支持的 JSON 节点类型: {element.ValueKind}");
		}
	}

	// 尝试获取列表元素类型。
	private static bool tryGetListElementType(Type type, out Type elementType)
	{
		if (type.IsArray)
		{
			elementType = type.GetElementType()
				?? throw new InvalidOperationException($"数组元素类型不可用: {type.FullName}");
			return true;
		}

		Type? listInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
			? type
			: type.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

		if (listInterface == null)
		{
			elementType = typeof(object);
			return false;
		}

		elementType = listInterface.GetGenericArguments()[0];
		return true;
	}

	// 尝试获取字典键值类型。
	private static bool tryGetDictionaryTypes(Type type, out Type keyType, out Type valueType)
	{
		Type? dictInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)
			? type
			: type.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

		if (dictInterface == null)
		{
			keyType = typeof(object);
			valueType = typeof(object);
			return false;
		}

		Type[] args = dictInterface.GetGenericArguments();
		keyType = args[0];
		valueType = args[1];
		return true;
	}

	// 字典键支持基础类型和枚举。
	private static bool isSupportedDictionaryKeyType(Type type)
	{
		return isBasicType(type) || type.IsEnum;
	}

	// 基础类型转换。
	private static object convertBasic(object value, Type targetType)
	{
		if (targetType == typeof(string))
		{
			return value.ToString()
				?? throw new InvalidOperationException("字符串转换失败");
		}

		if (targetType == typeof(bool))
		{
			return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(byte))
		{
			return Convert.ToByte(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(sbyte))
		{
			return Convert.ToSByte(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(short))
		{
			return Convert.ToInt16(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(ushort))
		{
			return Convert.ToUInt16(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(int))
		{
			return Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(uint))
		{
			return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(long))
		{
			return Convert.ToInt64(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(ulong))
		{
			return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(float))
		{
			return Convert.ToSingle(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(double))
		{
			return Convert.ToDouble(value, CultureInfo.InvariantCulture);
		}

		if (targetType == typeof(decimal))
		{
			return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		}

		throw new InvalidOperationException($"不支持的基础类型: {targetType.FullName}");
	}

	// 创建列表实例。
	private static IList createList(Type declaredListType, Type elementType)
	{
		if (declaredListType.IsArray)
		{
			object? arrayList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
			return arrayList as IList
				?? throw new InvalidOperationException($"无法创建列表实例: {declaredListType.FullName}");
		}

		if (declaredListType.IsInterface || declaredListType.IsAbstract)
		{
			object? interfaceList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
			return interfaceList as IList
				?? throw new InvalidOperationException($"无法创建列表实例: {declaredListType.FullName}");
		}

		object? list = Activator.CreateInstance(declaredListType);
		return list as IList
			?? throw new InvalidOperationException($"无法创建列表实例: {declaredListType.FullName}");
	}

	// 创建字典实例。
	private static IDictionary createDictionary(Type declaredDictType, Type keyType, Type valueType)
	{
		if (declaredDictType.IsInterface || declaredDictType.IsAbstract)
		{
			object? interfaceDict = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
			return interfaceDict as IDictionary
				?? throw new InvalidOperationException($"无法创建字典实例: {declaredDictType.FullName}");
		}

		object? dictionary = Activator.CreateInstance(declaredDictType);
		return dictionary as IDictionary
			?? throw new InvalidOperationException($"无法创建字典实例: {declaredDictType.FullName}");
	}
}
