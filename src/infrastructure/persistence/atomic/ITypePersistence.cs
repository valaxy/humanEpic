using System;

/// <summary>
/// 特定类型持久化处理器接口。
/// </summary>
internal interface ITypePersistence
{
	/// <summary>
	/// 当前处理器是否支持该类型。
	/// </summary>
	bool CanHandle(Type type);

	/// <summary>
	/// 序列化值。
	/// </summary>
	object Serialize(object value, Type declaredType);

	/// <summary>
	/// 反序列化值。
	/// </summary>
	object Deserialize(object rawValue, Type targetType);
}
