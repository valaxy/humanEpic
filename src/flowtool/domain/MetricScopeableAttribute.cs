using System;

/// <summary>
/// 将类标记为一个Metric的作用域
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MetricScopeableAttribute : Attribute
{
}