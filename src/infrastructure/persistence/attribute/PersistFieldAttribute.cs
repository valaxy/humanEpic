using System;

/// <summary>
/// 标记需要被持久化的字段。
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true)]
public sealed class PersistFieldAttribute : Attribute
{
	/// <summary>
	/// 序列化时字段名，未指定则使用原字段名。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 初始化字段特性。
	/// </summary>
	public PersistFieldAttribute(string? name = null)
	{
		Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
	}
}
