using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

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
		FlowToolXmlDocProvider xmlDocProvider = FlowToolXmlDocProvider.Load(assembly);

		IReadOnlyList<Type> flowTypes = assembly
			.GetTypes()
			.Where(static type => type.GetCustomAttributes(typeof(SystemDynamicsFlowAttribute), false).Length > 0)
			.ToList();

		IReadOnlyList<MethodInfo> processMethods = flowTypes
			.SelectMany(static type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
			.Where(static method => method.GetCustomAttributes(typeof(SystemDynamicsProcessAttribute), false).Length > 0)
			.Where(static method => method.IsSpecialName == false)
			.ToList();

		IReadOnlyList<FlowToolMetricNode> metricNodes = processMethods
			.Select(method => createMetricNode(method, xmlDocProvider))
			.ToList();

		IReadOnlyList<FlowToolEdge> edges = processMethods
			.SelectMany(method => createEdges(method, metricNodes))
			.Distinct()
			.ToList();

		return new FlowToolTopology(metricNodes, edges);
	}

	// 为流程方法创建指标节点。
	private static FlowToolMetricNode createMetricNode(MethodInfo method, FlowToolXmlDocProvider xmlDocProvider)
	{
		string declaringTypeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "UnknownType";
		string metricName = method.Name;
		string normalizedMetricName = normalizeMetricName(metricName);
		string nodeId = $"metric:{declaringTypeName}:{normalizedMetricName}";
		string displayName = xmlDocProvider.GetMethodSummary(method);
		string typeDisplayName = formatMetricTypeDisplayName(method.ReturnType);
		return new FlowToolMetricNode(nodeId, metricName, displayName, typeDisplayName, declaringTypeName);
	}

	// 构建指标节点之间的依赖边：参数节点 -> 方法指标节点。
	private static IReadOnlyList<FlowToolEdge> createEdges(MethodInfo method, IReadOnlyList<FlowToolMetricNode> metricNodes)
	{
		string targetNodeId = createMetricNodeId(method);
		string targetOwnerTypeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "UnknownType";
		IReadOnlyList<string> parameterMetricNames = method
			.GetParameters()
			.Where(static parameter => parameter.IsOut == false && parameter.ParameterType.IsByRef == false)
			.Select(static parameter => normalizeMetricName(parameter.Name ?? "value"))
			.Distinct(StringComparer.Ordinal)
			.ToList();

		return parameterMetricNames
			.SelectMany(parameterMetricName => resolveSourceMetrics(parameterMetricName, targetNodeId, targetOwnerTypeName, metricNodes))
			.Select(sourceNode => new FlowToolEdge(sourceNode.NodeId, targetNodeId))
			.ToList();
	}

	// 按“同名指标”规则解析参数来源指标，优先同类。
	private static IReadOnlyList<FlowToolMetricNode> resolveSourceMetrics(
		string parameterMetricName,
		string targetNodeId,
		string targetOwnerTypeName,
		IReadOnlyList<FlowToolMetricNode> metricNodes)
	{
		IReadOnlyList<FlowToolMetricNode> allCandidates = metricNodes
			.Where(metricNode => metricNode.NodeId != targetNodeId)
			.Where(metricNode => normalizeMetricName(metricNode.MetricName) == parameterMetricName)
			.ToList();
		IReadOnlyList<FlowToolMetricNode> sameOwnerCandidates = allCandidates
			.Where(metricNode => metricNode.OwnerTypeFullName == targetOwnerTypeName)
			.ToList();

		return sameOwnerCandidates.Count > 0
			? sameOwnerCandidates
			: allCandidates;
	}

	// 构建方法对应的指标 ID。
	private static string createMetricNodeId(MethodInfo method)
	{
		string declaringTypeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "UnknownType";
		string normalizedMetricName = normalizeMetricName(method.Name);
		return $"metric:{declaringTypeName}:{normalizedMetricName}";
	}

	// 将类型名格式化为更易读的显示文本。
	private static string formatMetricTypeDisplayName(Type type)
	{
		return type.FullName switch
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
			"System.Void" => "void",
			_ => type.Name
		};
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

	// XML 文档提供器。
	private sealed class FlowToolXmlDocProvider
	{
		// 工程根目录。
		private readonly string? projectRoot;
		// 方法摘要缓存。
		private readonly Dictionary<string, string> summaryByMethodKey = new(StringComparer.Ordinal);

		private FlowToolXmlDocProvider(string? projectRoot)
		{
			this.projectRoot = projectRoot;
		}

		// 载入工程上下文。
		public static FlowToolXmlDocProvider Load(Assembly assembly)
		{
			string? root = resolveProjectRoot(AppContext.BaseDirectory);
			if (string.IsNullOrWhiteSpace(root))
			{
				return new FlowToolXmlDocProvider(null);
			}

			return new FlowToolXmlDocProvider(root);
		}

		// 读取方法摘要，缺失时回退为方法名。
		public string GetMethodSummary(MethodInfo method)
		{
			string methodKey = createMethodKey(method);
			if (summaryByMethodKey.TryGetValue(methodKey, out string? cachedSummary))
			{
				return cachedSummary;
			}

			string resolvedSummary = tryReadSummaryFromSource(method) ?? method.Name;
			summaryByMethodKey[methodKey] = resolvedSummary;
			return resolvedSummary;
		}

		// 生成方法缓存键。
		private static string createMethodKey(MethodInfo method)
		{
			string typeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "UnknownType";
			return $"{typeName}.{method.Name}";
		}

		// 从源码中读取 XML 注释摘要。
		private string? tryReadSummaryFromSource(MethodInfo method)
		{
			if (string.IsNullOrWhiteSpace(projectRoot) || method.DeclaringType is null)
			{
				return null;
			}

			string sourceFilePath = Path.Combine(projectRoot!, "src", "flowtool", $"{method.DeclaringType.Name}.cs");
			if (File.Exists(sourceFilePath) == false)
			{
				return null;
			}

			string sourceText = File.ReadAllText(sourceFilePath);
			string methodNamePattern = Regex.Escape(method.Name);
			Regex methodSummaryRegex = new(
				$"(?s)///\\s*<summary>\\s*(?<summary>.*?)\\s*///\\s*</summary>\\s*(?:\\s*///.*?\\r?\\n)*\\s*\\[SystemDynamicsProcess\\]\\s*(?:public|private|protected|internal)\\s+[^\\r\\n]*?\\b{methodNamePattern}\\s*\\(",
				RegexOptions.Compiled);
			Match match = methodSummaryRegex.Match(sourceText);
			if (match.Success == false)
			{
				return null;
			}

			return normalizeSummaryText(match.Groups["summary"].Value);
		}

		// 解析工程根目录。
		private static string? resolveProjectRoot(string startDirectory)
		{
			DirectoryInfo? currentDirectory = new(startDirectory);
			while (currentDirectory is not null)
			{
				bool hasProjectFile = currentDirectory
					.GetFiles("*.csproj", SearchOption.TopDirectoryOnly)
					.Any();
				if (hasProjectFile)
				{
					return currentDirectory.FullName;
				}

				currentDirectory = currentDirectory.Parent;
			}

			return null;
		}

		// 规范化 XML 摘要文本。
		private static string normalizeSummaryText(string rawSummary)
		{
			Regex summaryLineRegex = new("^\\s*///\\s?", RegexOptions.Compiled);
			return string.Join(
				" ",
				rawSummary
					.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
					.Select(line => summaryLineRegex.Replace(line, string.Empty).Trim())
					.Where(static line => string.IsNullOrWhiteSpace(line) == false));
		}
	}
}
