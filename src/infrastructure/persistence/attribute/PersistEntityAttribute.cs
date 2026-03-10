using System;

/// <summary>
/// 标记实体类型，并声明其归属的集合类型。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PersistEntityAttribute : Attribute
{
	/// <summary>
	/// 该实体对应的集合类型。
	/// </summary>
	public Type CollectionType { get; }

	/// <summary>
	/// 初始化实体持久化标记。
	/// </summary>
	public PersistEntityAttribute(Type collectionType)
	{
		CollectionType = collectionType;
	}
}
