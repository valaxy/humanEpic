using System;
using System.Globalization;

internal sealed class DoubleTypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => type == typeof(double);
	public object Serialize(object value, Type declaredType) => value;
	public object Deserialize(object rawValue, Type targetType) => Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
}
