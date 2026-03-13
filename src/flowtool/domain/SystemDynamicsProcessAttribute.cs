using System;

/// <summary>
/// 标记系统动力学过程方法，供 flowtool 反射提取。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class SystemDynamicsProcessAttribute : Attribute
{
}
