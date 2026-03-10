using System;
using System.Globalization;

internal sealed class SingleTypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => type == typeof(float);
	public object Serialize(object value, Type declaredType) => value;
	public object Deserialize(object rawValue, Type targetType) => Convert.ToSingle(rawValue, CultureInfo.InvariantCulture);
}
