using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Godot;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
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

	// 判断是否为值元组类型。
	private static bool isValueTupleType(Type type)
	{
		if (!type.IsValueType || !type.IsGenericType)
		{
			return false;
		}

		Type genericType = type.GetGenericTypeDefinition();
		return genericType == typeof(ValueTuple<>)
			|| genericType == typeof(ValueTuple<,>)
			|| genericType == typeof(ValueTuple<,,>)
			|| genericType == typeof(ValueTuple<,,,>)
			|| genericType == typeof(ValueTuple<,,,,>)
			|| genericType == typeof(ValueTuple<,,,,,>)
			|| genericType == typeof(ValueTuple<,,,,,,>)
			|| genericType == typeof(ValueTuple<,,,,,,,>);
	}

	// 获取值元组元素类型。
	private static Type[] getValueTupleElementTypes(Type tupleType)
	{
		if (!isValueTupleType(tupleType))
		{
			throw new InvalidOperationException($"类型不是值元组: {tupleType.FullName}");
		}

		return tupleType.GetGenericArguments();
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

	// 获取被持久化标记的属性集合。
	private static List<(PropertyInfo property, PersistPropertyAttribute attr)> getPersistProperties(Type type)
	{
		return type
			.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Select(property => (property, attr: property.GetCustomAttribute<PersistPropertyAttribute>(true)))
			.Where(item => item.attr != null)
			.Select(item => (item.property, item.attr!))
			.ToList();
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

	// 尝试获取集合元素类型（HashSet/ISet）。
	private static bool tryGetSetElementType(Type type, out Type elementType)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
		{
			elementType = type.GetGenericArguments()[0];
			return true;
		}

		Type? setInterface = type.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISet<>));

		if (setInterface == null)
		{
			elementType = typeof(object);
			return false;
		}

		elementType = setInterface.GetGenericArguments()[0];
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

	// 创建集合实例。
	private static object createSet(Type declaredSetType, Type elementType)
	{
		if (declaredSetType.IsInterface || declaredSetType.IsAbstract)
		{
			return Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType))
				?? throw new InvalidOperationException($"无法创建集合实例: {declaredSetType.FullName}");
		}

		return Activator.CreateInstance(declaredSetType)
			?? throw new InvalidOperationException($"无法创建集合实例: {declaredSetType.FullName}");
	}

	// 判断是否为受支持的 Godot 值类型。
	private static bool isSupportedGodotValueType(Type type)
	{
		return type == typeof(Color)
			|| type == typeof(Vector2)
			|| type == typeof(Vector2I);
	}

	// 序列化 Godot 值类型。
	private static Dictionary<string, object> serializeGodotValue(object value, Type type)
	{
		if (type == typeof(Color))
		{
			Color color = (Color)value;
			return new Dictionary<string, object>
			{
				{ godotTag, true },
				{ godotType, nameof(Color) },
				{ godotData, new Dictionary<string, object>
					{
						{ "r", color.R },
						{ "g", color.G },
						{ "b", color.B },
						{ "a", color.A }
					}
				}
			};
		}

		if (type == typeof(Vector2))
		{
			Vector2 vector = (Vector2)value;
			return new Dictionary<string, object>
			{
				{ godotTag, true },
				{ godotType, nameof(Vector2) },
				{ godotData, new Dictionary<string, object>
					{
						{ "x", vector.X },
						{ "y", vector.Y }
					}
				}
			};
		}

		if (type == typeof(Vector2I))
		{
			Vector2I vector = (Vector2I)value;
			return new Dictionary<string, object>
			{
				{ godotTag, true },
				{ godotType, nameof(Vector2I) },
				{ godotData, new Dictionary<string, object>
					{
						{ "x", vector.X },
						{ "y", vector.Y }
					}
				}
			};
		}

		throw new InvalidOperationException($"不支持的 Godot 值类型: {type.FullName}");
	}

	// 反序列化 Godot 值类型。
	private static object deserializeGodotValue(object rawValue, Type targetType)
	{
		if (rawValue is not Dictionary<string, object> node)
		{
			throw new InvalidOperationException($"Godot 值类型数据结构非法: {targetType.FullName}");
		}

		if (!node.ContainsKey(godotTag) || !node.ContainsKey(godotType) || !node.ContainsKey(godotData))
		{
			throw new InvalidOperationException($"Godot 值类型缺少必要键: {targetType.FullName}");
		}

		if (node[godotData] is not Dictionary<string, object> data)
		{
			throw new InvalidOperationException($"Godot 值类型 data 结构非法: {targetType.FullName}");
		}

		if (targetType == typeof(Color))
		{
			float r = Convert.ToSingle(data["r"], CultureInfo.InvariantCulture);
			float g = Convert.ToSingle(data["g"], CultureInfo.InvariantCulture);
			float b = Convert.ToSingle(data["b"], CultureInfo.InvariantCulture);
			float a = Convert.ToSingle(data["a"], CultureInfo.InvariantCulture);
			return new Color(r, g, b, a);
		}

		if (targetType == typeof(Vector2))
		{
			float x = Convert.ToSingle(data["x"], CultureInfo.InvariantCulture);
			float y = Convert.ToSingle(data["y"], CultureInfo.InvariantCulture);
			return new Vector2(x, y);
		}

		if (targetType == typeof(Vector2I))
		{
			int x = Convert.ToInt32(data["x"], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(data["y"], CultureInfo.InvariantCulture);
			return new Vector2I(x, y);
		}

		throw new InvalidOperationException($"不支持的 Godot 值类型: {targetType.FullName}");
	}
}
