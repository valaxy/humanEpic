using System;
using System.Globalization;

internal sealed class DecimalTypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => type == typeof(decimal);
	public object Serialize(object value, Type declaredType) => value;
	public object Deserialize(object rawValue, Type targetType) => Convert.ToDecimal(rawValue, CultureInfo.InvariantCulture);
}
