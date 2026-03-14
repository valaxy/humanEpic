using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

/// <summary>
/// 通过这个类读取方法中的XML文档注释
/// </summary>
public static class XmlDocReader
{
    // XML 文档缓存。
    private static readonly Lazy<IReadOnlyDictionary<string, string>> xmlSummaries = new(loadXmlSummaries);

    /// <summary>
    /// 获取方法中的XML.summary注释，如果不存在返回空值
    /// </summary>
    public static string GetXmlSummary(MethodInfo method)
    {
        if (method.DeclaringType is null)
        {
            return string.Empty;
        }

        string memberName = buildMethodMemberName(method);
        if (xmlSummaries.Value.TryGetValue(memberName, out string? summary))
        {
            return summary;
        }

        throw new InvalidOperationException($"未找到方法的 XML 注释: {method.DeclaringType.FullName}.{method.Name}");
    }

    /// <summary>
    /// 获取类型中的XML.summary注释，如果不存在返回空值
    /// </summary>
    public static string GetXmlSummary(Type type)
    {
        string memberName = $"T:{buildXmlTypeName(type)}";
        if (xmlSummaries.Value.TryGetValue(memberName, out string? summary))
        {
            return summary;
        }

        throw new InvalidOperationException($"未找到类型的 XML 注释: {buildXmlTypeName(type)}");
    }




    // 读取并缓存 XML 注释。
    private static IReadOnlyDictionary<string, string> loadXmlSummaries()
    {
        string xmlFilePath = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(xmlFilePath) || File.Exists(xmlFilePath) == false)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        XDocument doc = XDocument.Load(xmlFilePath);
        IReadOnlyDictionary<string, string> summaries = doc
            .Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .Select(member => new
            {
                Name = member.Attribute("name")!.Value,
                Summary = normalizeWhitespace(member.Element("summary")?.Value ?? string.Empty)
            })
            .Where(item => string.IsNullOrWhiteSpace(item.Summary) == false)
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Summary, StringComparer.Ordinal);

        return summaries;
    }

    // 生成方法在 XML 文档中的成员名。
    private static string buildMethodMemberName(MethodInfo method)
    {
        string declaringTypeName = buildXmlTypeName(method.DeclaringType!);
        string parameterText = string.Join(",", method.GetParameters().Select(parameter => buildXmlTypeName(parameter.ParameterType)));
        if (string.IsNullOrWhiteSpace(parameterText))
        {
            return $"M:{declaringTypeName}.{method.Name}";
        }

        return $"M:{declaringTypeName}.{method.Name}({parameterText})";
    }

    // 构造 XML 类型名。
    private static string buildXmlTypeName(Type type)
    {
        if (type.IsByRef)
        {
            Type elementType = type.GetElementType()!;
            return $"{buildXmlTypeName(elementType)}@";
        }

        if (type.IsArray)
        {
            Type elementType = type.GetElementType()!;
            return $"{buildXmlTypeName(elementType)}[]";
        }

        string rawName = type.FullName ?? type.Name;
        return rawName.Replace('+', '.');
    }

    // 压缩注释中的空白字符。
    private static string normalizeWhitespace(string input)
    {
        IReadOnlyList<string> lines = input
            .Split('\n')
            .Select(static line => line.Trim())
            .Where(line => string.IsNullOrWhiteSpace(line) == false)
            .ToList();
        return string.Join(" ", lines);
    }
}