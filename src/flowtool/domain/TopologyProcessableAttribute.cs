using System;

/// <summary>
/// 标记一个方法是可处理的
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TopologyProcessableAttribute : Attribute
{
}
