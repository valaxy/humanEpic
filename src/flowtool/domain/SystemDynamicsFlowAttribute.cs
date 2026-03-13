using System;

/// <summary>
/// 标记允许被 flowtool 反射提取的系统动力学类。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SystemDynamicsFlowAttribute : Attribute
{
}