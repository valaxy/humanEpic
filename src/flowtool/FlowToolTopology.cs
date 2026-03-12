using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// flowtool 拓扑快照。
/// </summary>
public sealed record FlowToolTopology(
	IReadOnlyList<FlowToolProcessNode> Processes,
	IReadOnlyList<FlowToolMetricNode> Metrics,
	IReadOnlyList<FlowToolEdge> Edges
);

/// <summary>
/// flowtool 过程节点定义。
/// </summary>
public sealed record FlowToolProcessNode(
	string NodeId,
	string DisplayName,
	IReadOnlyList<string> InputMetricIds,
	string? OutputMetricId
);

/// <summary>
/// flowtool 指标节点定义。
/// </summary>
public sealed record FlowToolMetricNode(
	string NodeId,
	string DisplayName,
	string TypeDisplayName
);

/// <summary>
/// flowtool 有向边定义。
/// </summary>
public sealed record FlowToolEdge(
	string FromNodeId,
	string ToNodeId
);

/// <summary>
/// 基于反射提取系统动力学拓扑。
/// </summary>
public sealed class FlowToolTopologyExtractor
{
	/// <summary>
	/// 扫描当前程序集并提取被 SystemDynamicsProcessAttribute 标记的方法。
	/// </summary>
	public FlowToolTopology ExtractFromCurrentAssembly()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		IReadOnlyList<Type> flowTypes = assembly
			.GetTypes()
			.Where(static type => type.GetCustomAttributes(typeof(SystemDynamicsFlowAttribute), false).Length > 0)
			.ToList();

		IReadOnlyList<MethodInfo> processMethods = flowTypes
			.SelectMany(static type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
			.Where(static method => method.GetCustomAttributes(typeof(SystemDynamicsProcessAttribute), false).Length > 0)
			.Where(static method => method.IsSpecialName == false)
			.ToList();

		IReadOnlyList<FlowToolProcessNode> processNodes = processMethods
			.Select(createProcessNode)
			.ToList();

		IReadOnlyList<FlowToolMetricNode> metricNodes = processNodes
			.SelectMany(static process => process.InputMetricIds.Concat(process.OutputMetricId is null ? Array.Empty<string>() : new[] { process.OutputMetricId }))
			.Distinct(StringComparer.Ordinal)
			.Select(createMetricNode)
			.ToList();

		IReadOnlyList<FlowToolEdge> edges = processNodes
			.SelectMany(createEdges)
			.ToList();

		return new FlowToolTopology(processNodes, metricNodes, edges);
	}

	// 构建过程节点。
	private static FlowToolProcessNode createProcessNode(MethodInfo method)
	{
		string declaringTypeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "UnknownType";
		string processNodeId = $"process:{declaringTypeName}.{method.Name}";
		IReadOnlyList<string> inputMetricIds = method
			.GetParameters()
			.Where(static parameter => parameter.IsOut == false && parameter.ParameterType.IsByRef == false)
			.Select(createInputMetricId)
			.Distinct(StringComparer.Ordinal)
			.ToList();

		string? outputMetricId = method.ReturnType == typeof(void)
			? null
			: createOutputMetricId(method);

		string displayName = $"{method.DeclaringType?.Name}.{method.Name}";
		return new FlowToolProcessNode(processNodeId, displayName, inputMetricIds, outputMetricId);
	}

	// 构建指标节点。
	private static FlowToolMetricNode createMetricNode(string metricNodeId)
	{
		string[] segments = metricNodeId.Split(':', StringSplitOptions.None);
		string displayName = segments.Length >= 3
			? segments[2]
			: metricNodeId.Replace("metric:", string.Empty, StringComparison.Ordinal);
		string typeDisplayName = segments.Length >= 2
			? formatMetricTypeDisplayName(segments[1])
			: "Unknown";
		return new FlowToolMetricNode(metricNodeId, displayName, typeDisplayName);
	}

	// 将指标类型名格式化为更易读的显示文本。
	private static string formatMetricTypeDisplayName(string typeName)
	{
		return typeName switch
		{
			"System.Boolean" => "bool",
			"System.Byte" => "byte",
			"System.Decimal" => "decimal",
			"System.Double" => "double",
			"System.Int16" => "short",
			"System.Int32" => "int",
			"System.Int64" => "long",
			"System.Single" => "float",
			"System.String" => "string",
			_ => typeName.Split('.').Last()
		};
	}

	// 根据过程节点构建输入/输出边。
	private static IEnumerable<FlowToolEdge> createEdges(FlowToolProcessNode processNode)
	{
		IReadOnlyList<FlowToolEdge> inputEdges = processNode.InputMetricIds
			.Select(metricId => new FlowToolEdge(metricId, processNode.NodeId))
			.ToList();

		IReadOnlyList<FlowToolEdge> outputEdges = processNode.OutputMetricId is null
			? Array.Empty<FlowToolEdge>()
			: new[] { new FlowToolEdge(processNode.NodeId, processNode.OutputMetricId) };

		return inputEdges.Concat(outputEdges);
	}

	// 构建输入指标 ID。
	private static string createInputMetricId(ParameterInfo parameterInfo)
	{
		Type parameterType = parameterInfo.ParameterType;
		string typeName = parameterType.FullName ?? parameterType.Name;
		string parameterName = string.IsNullOrWhiteSpace(parameterInfo.Name) ? "value" : parameterInfo.Name;
		string metricName = normalizeMetricName(parameterName);
		return $"metric:{typeName}:{metricName}";
	}

	// 构建输出指标 ID。
	private static string createOutputMetricId(MethodInfo method)
	{
		Type returnType = method.ReturnType;
		string typeName = returnType.FullName ?? returnType.Name;
		string metricName = normalizeMetricName(method.Name);
		return $"metric:{typeName}:{metricName}";
	}

	// 统一指标命名，确保过程输出与输入参数可以按名称对齐。
	private static string normalizeMetricName(string metricName)
	{
		if (string.IsNullOrWhiteSpace(metricName))
		{
			return "value";
		}

		string trimmedName = metricName.Trim();
		if (trimmedName.Length == 1)
		{
			return trimmedName.ToLowerInvariant();
		}

		return char.ToLowerInvariant(trimmedName[0]) + trimmedName[1..];
	}
}
