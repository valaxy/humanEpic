using System;

/// <summary>
/// 标记需要被持久化的属性。
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class PersistPropertyAttribute : Attribute
{
	/// <summary>
	/// 序列化时属性名，未指定则使用原属性名。
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 初始化属性特性。
	/// </summary>
	public PersistPropertyAttribute(string? name = null)
	{
		Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
	}
}