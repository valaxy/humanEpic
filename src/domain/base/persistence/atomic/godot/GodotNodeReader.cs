using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Godot 值类型节点解析辅助。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	internal static Dictionary<string, object> readGodotNode(object rawValue, string targetTypeName)
	{
		if (rawValue is not Dictionary<string, object> node)
		{
			throw new InvalidOperationException($"Godot 值类型数据结构非法: {targetTypeName}");
		}

		if (!node.ContainsKey("__godot") || !node.ContainsKey("type"))
		{
			throw new InvalidOperationException($"Godot 值类型缺少必要键: {targetTypeName}");
		}

		if (node["type"].ToString() != targetTypeName)
		{
			throw new InvalidOperationException($"Godot 值类型不匹配: 期望 {targetTypeName}");
		}

		if (node.TryGetValue("data", out object? legacyDataRaw))
		{
			if (legacyDataRaw is not Dictionary<string, object> legacyData)
			{
				throw new InvalidOperationException($"Godot 值类型 data 结构非法: {targetTypeName}");
			}

			return legacyData;
		}

		return node
			.Where(pair => pair.Key != "__godot" && pair.Key != "type")
			.ToDictionary(pair => pair.Key, pair => pair.Value);
	}
}
