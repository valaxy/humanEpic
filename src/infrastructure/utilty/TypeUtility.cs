using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GDictionary = Godot.Collections.Dictionary;

/// <summary>
/// 类型相关通用工具，包含类型转换能力
/// </summary>
public static class TypeUtility
{
	/// <summary>
	/// 将 int 键字典转换为 GDictionary。
	/// </summary>
	public static GDictionary ToGDictionary<TValue>(Dictionary<int, TValue> source)
	{
		GDictionary result = new GDictionary();
		source
			.ToList()
			.ForEach(pair => result[Variant.From(pair.Key)] = Variant.From(pair.Value));

		return result;
	}

	/// <summary>
	/// 将 string 键字典转换为 GDictionary。
	/// </summary>
	public static GDictionary ToGDictionary<TValue>(Dictionary<string, TValue> source)
	{
		GDictionary result = new GDictionary();
		source
			.ToList()
			.ForEach(pair => result[Variant.From(pair.Key)] = Variant.From(pair.Value));

		return result;
	}

	/// <summary>
	/// 将枚举键字典转换为 GDictionary，键按底层 int 存储。
	/// </summary>
	public static GDictionary ToGDictionary<TEnum, TValue>(Dictionary<TEnum, TValue> source) where TEnum : struct, Enum
	{
		GDictionary result = new GDictionary();
		source
			.ToList()
			.ForEach(pair => result[Variant.From(Convert.ToInt32(pair.Key))] = Variant.From(pair.Value));

		return result;
	}
}
