using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// 基于反射提取系统动力学拓扑�?
/// </summary>
public static class TopologyExtractor
{
	/// <summary>
	/// 扫描当前程序集并提取�?SystemDynamicsProcessAttribute 标记的方法�?
	/// </summary>
	public static GameSystem ExtractFromCurrentAssembly()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		XmlDocProvider xmlDocProvider = XmlDocProvider.Load(assembly);

		IReadOnlyList<Type> flowTypes = assembly
			.GetTypes()
			.Where(static type => type.GetCustomAttributes(typeof(TopologyScopeableAttribute), false).Length > 0)
			.ToList();

		IReadOnlyList<MethodInfo> processMethods = flowTypes
			.SelectMany(static type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
			.Where(static method => method.GetCustomAttributes(typeof(TopologyProcessableAttribute), false).Length > 0)
			.Where(static method => method.IsSpecialName == false)
			.ToList();

		IReadOnlyList<MetricNode> metricNodes = processMethods
			.Select(method => createMetricNode(method, xmlDocProvider))
			.ToList();

		IReadOnlyList<MetricEdge> edges = processMethods
			.SelectMany(method => createEdges(method, metricNodes))
			.Distinct()
			.ToList();

		return new GameSystem(metricNodes, edges);
	}

	// 为流程方法创建指标节点�?
	private static MetricNode createMetricNode(MethodInfo method, XmlDocProvider xmlDocProvider)
	{
		string declaringTypeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "UnknownType";
		string metricName = method.Name;
		string normalizedMetricName = normalizeMetricName(metricName);
		string nodeId = $"metric:{declaringTypeName}:{normalizedMetricName}";
		string displayName = xmlDocProvider.GetMethodSummary(method);
		string typeDisplayName = formatMetricTypeDisplayName(method.ReturnType);
		return new MetricNode(nodeId, metricName, displayName, typeDisplayName, declaringTypeName);
	}

	// 构建指标节点之间的依赖边：参数节�?-> 方法指标节点�?
	private static IReadOnlyList<MetricEdge> createEdges(MethodInfo method, IReadOnlyList<MetricNode> metricNodes)
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
			.Select(sourceNode => new MetricEdge(sourceNode.NodeId, targetNodeId))
			.ToList();
	}

	// 按“同名指标”规则解析参数来源指标，优先同类�?
	private static IReadOnlyList<MetricNode> resolveSourceMetrics(
		string parameterMetricName,
		string targetNodeId,
		string targetOwnerTypeName,
		IReadOnlyList<MetricNode> metricNodes)
	{
		IReadOnlyList<MetricNode> allCandidates = metricNodes
			.Where(metricNode => normalizeMetricName(metricNode.MetricName) == parameterMetricName)
			.ToList();
		IReadOnlyList<MetricNode> sameOwnerCandidates = allCandidates
			.Where(metricNode => metricNode.OwnerTypeFullName == targetOwnerTypeName)
			.ToList();

		return sameOwnerCandidates.Count > 0
			? sameOwnerCandidates
			: allCandidates;
	}

	// 构建方法对应的指�?ID�?
	private static string createMetricNodeId(MethodInfo method)
	{
		string declaringTypeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "UnknownType";
		string normalizedMetricName = normalizeMetricName(method.Name);
		return $"metric:{declaringTypeName}:{normalizedMetricName}";
	}

	// 将类型名格式化为完整显示文本�?
	private static string formatMetricTypeDisplayName(Type type)
	{
		if (type.IsByRef)
		{
			Type? elementType = type.GetElementType();
			return elementType is null
				? "UnknownByRefType"
				: $"{formatMetricTypeDisplayName(elementType)}&";
		}

		if (type.IsArray)
		{
			Type? elementType = type.GetElementType();
			return elementType is null
				? "UnknownArrayType[]"
				: $"{formatMetricTypeDisplayName(elementType)}[]";
		}

		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			Type? nullableElementType = Nullable.GetUnderlyingType(type);
			return nullableElementType is null
				? "System.Nullable<?>"
				: $"{formatMetricTypeDisplayName(nullableElementType)}?";
		}

		if (type.IsGenericType)
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			string genericTypeName = (genericTypeDefinition.FullName ?? genericTypeDefinition.Name).Split('`')[0];
			string genericArguments = string.Join(
				", ",
				type.GetGenericArguments().Select(formatMetricTypeDisplayName));
			return $"{genericTypeName}<{genericArguments}>";
		}

		return type.FullName ?? type.Name;
	}

	// 统一指标命名，确保过程输出与输入参数可以按名称对齐�?
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

	// XML 文档提供器�?
	private sealed class XmlDocProvider
	{
		// 工程根目录�?
		private readonly string? projectRoot;
		// 方法摘要缓存�?
		private readonly Dictionary<string, string> summaryByMethodKey = new(StringComparer.Ordinal);

		private XmlDocProvider(string? projectRoot)
		{
			this.projectRoot = projectRoot;
		}

		// 载入工程上下文�?
		public static XmlDocProvider Load(Assembly assembly)
		{
			string? root = resolveProjectRoot(AppContext.BaseDirectory);
			if (string.IsNullOrWhiteSpace(root))
			{
				return new XmlDocProvider(null);
			}

			return new XmlDocProvider(root);
		}

		// 读取方法摘要，缺失时回退为方法名�?
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

		// 生成方法缓存键�?
		private static string createMethodKey(MethodInfo method)
		{
			string typeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "UnknownType";
			return $"{typeName}.{method.Name}";
		}

		// 从源码中读取 XML 注释摘要�?
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

		// 解析工程根目录�?
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

		// 规范�?XML 摘要文本�?
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
