using System;

/// <summary>
/// 标记可被领域持久化器处理的模型类型。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PersistableAttribute : Attribute
{
}
