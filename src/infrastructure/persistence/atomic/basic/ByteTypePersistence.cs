using System;
using System.Globalization;

internal sealed class ByteTypePersistence : ITypePersistence
{
	public bool CanHandle(Type type) => type == typeof(byte);
	public object Serialize(object value, Type declaredType) => value;
	public object Deserialize(object rawValue, Type targetType) => Convert.ToByte(rawValue, CultureInfo.InvariantCulture);
}
