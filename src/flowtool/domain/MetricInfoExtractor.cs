using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// 基于反射提取Metric相关的信息
/// </summary>
public static class MetricInfoExtractor
{
	/// <summary>
	/// 扫描当前程序集并提取 MetricScopeable 标记的方法。
	/// </summary>
	public static GameSystem ExtractFromCurrentAssembly()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();

		IReadOnlyList<MetricScope> captureTypes = assembly
			.GetTypes()
			.Where(static type => type.GetCustomAttributes(typeof(MetricScopeableAttribute), false).Length > 0)
			.Select(static type => createMetricScope(type))
			.ToList();

		return new GameSystem(captureTypes);
	}

	private static MetricScope createMetricScope(Type type)
	{
		string name = type.Name;
		string displayName = XmlDocReader.GetXmlSummary(type);

		IReadOnlyList<MethodInfo> captureMethods = type
			.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Where(static method => method.GetCustomAttributes(typeof(MetricableAttribute), false).Length > 0)
			.Where(static method => method.IsSpecialName == false)
			.ToList();

		IReadOnlyList<Metric> metrics = captureMethods
			.Select(createMetric)
			.ToList();

		IReadOnlyList<(string input, string output)> relations = captureMethods
			.SelectMany(createEdges)
			.Distinct()
			.ToList();

		return new MetricScope(name, displayName, metrics, relations);
	}

	// 为方法创建指标。
	private static Metric createMetric(MethodInfo method)
	{
		string typeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "UnknownType";
		string metricName = method.Name;
		string displayName = XmlDocReader.GetXmlSummary(method);
		return new Metric(metricName, displayName, typeName);
	}


	// 构建指标之间的关系
	private static IList<(string input, string output)> createEdges(MethodInfo method)
	{
		IReadOnlyList<string> inputNames = method
			.GetParameters()
			.Where(static parameter => parameter.IsOut == false && parameter.ParameterType.IsByRef == false)
			.Select(static parameter => parameter.Name ?? "")
			.Distinct(StringComparer.Ordinal)
			.ToList();

		string outputName = method.Name;

		return inputNames
			.Select(inputName => (input: inputName, output: outputName))
			.ToList();
	}

}
