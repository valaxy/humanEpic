using System;
using System.Collections.Generic;

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

		if (!node.ContainsKey("__godot") || !node.ContainsKey("type") || !node.ContainsKey("data"))
		{
			throw new InvalidOperationException($"Godot 值类型缺少必要键: {targetTypeName}");
		}

		if (node["data"] is not Dictionary<string, object> data)
		{
			throw new InvalidOperationException($"Godot 值类型 data 结构非法: {targetTypeName}");
		}

		return data;
	}
}
