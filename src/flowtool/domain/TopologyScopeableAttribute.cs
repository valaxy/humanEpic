using System;

/// <summary>
/// 将类标记为一个可识别范围
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TopologyScopeableAttribute : Attribute
{
}