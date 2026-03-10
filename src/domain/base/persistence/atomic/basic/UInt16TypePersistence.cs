using System;
using System.Globalization;

internal sealed class UInt16TypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => type == typeof(ushort);
	public object Serialize(object value, Type declaredType) => value;
	public object Deserialize(object rawValue, Type targetType) => Convert.ToUInt16(rawValue, CultureInfo.InvariantCulture);
}
