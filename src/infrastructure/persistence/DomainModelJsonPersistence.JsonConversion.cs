using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
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
}
