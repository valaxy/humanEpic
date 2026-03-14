using System;
using System.Text;

/// <summary>
/// 定义一个指标，包含指标具体的不变信息
/// </summary>
public sealed class Metric
{
	/// <summary>
	/// 指标英文名（方法名）。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 指标显示名（通常来自 XML 注释）。
	/// </summary>
	public string DisplayName { get; }

	/// <summary>
	/// 所属类型全名。
	/// </summary>
	public string TypeFullName { get; }


	/// <summary>
	/// 创建指标节点。
	/// </summary>
	public Metric(string name, string displayName, string typeFullName)
	{
		Name = name;
		DisplayName = displayName;
		TypeFullName = typeFullName;
	}

	/// <summary>
	/// 创建节点详情文本。
	/// </summary>
	public string GetDetailText()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append($"名称：{Name}");
		if (!string.IsNullOrEmpty(DisplayName))
		{
			sb.Append($"显示名：{DisplayName}");
		}
		sb.Append($"类型：{TypeFullName}");
		return sb.ToString();
	}
}


// // 将类型名格式化为完整显示文本。
// private static string formatMetricTypeDisplayName(Type type)
// {
// 	if (type.IsByRef)
// 	{
// 		Type? elementType = type.GetElementType();
// 		return elementType is null
// 			? "UnknownByRefType"
// 			: $"{formatMetricTypeDisplayName(elementType)}&";
// 	}

// 	if (type.IsArray)
// 	{
// 		Type? elementType = type.GetElementType();
// 		return elementType is null
// 			? "UnknownArrayType[]"
// 			: $"{formatMetricTypeDisplayName(elementType)}[]";
// 	}

// 	if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
// 	{
// 		Type? nullableElementType = Nullable.GetUnderlyingType(type);
// 		return nullableElementType is null
// 			? "System.Nullable<?>"
// 			: $"{formatMetricTypeDisplayName(nullableElementType)}?";
// 	}

// 	if (type.IsGenericType)
// 	{
// 		Type genericTypeDefinition = type.GetGenericTypeDefinition();
// 		string genericTypeName = (genericTypeDefinition.FullName ?? genericTypeDefinition.Name).Split('`')[0];
// 		string genericArguments = string.Join(", ", type.GetGenericArguments().Select(formatMetricTypeDisplayName));
// 		return $"{genericTypeName}<{genericArguments}>";
// 	}

// 	return type.FullName ?? type.Name;
// }
