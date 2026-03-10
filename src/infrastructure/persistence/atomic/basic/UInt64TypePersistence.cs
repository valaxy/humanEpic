using System;
using System.Globalization;

internal sealed class UInt64TypePersistence : ITypePersistence
{
	public bool CanHandle(Type type) => type == typeof(ulong);
	public object Serialize(object value, Type declaredType) => value;
	public object Deserialize(object rawValue, Type targetType) => Convert.ToUInt64(rawValue, CultureInfo.InvariantCulture);
}
