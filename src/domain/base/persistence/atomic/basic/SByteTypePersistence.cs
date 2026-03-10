using System;
using System.Globalization;

internal sealed class SByteTypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => type == typeof(sbyte);
	public object Serialize(object value, Type declaredType) => value;
	public object Deserialize(object rawValue, Type targetType) => Convert.ToSByte(rawValue, CultureInfo.InvariantCulture);
}
