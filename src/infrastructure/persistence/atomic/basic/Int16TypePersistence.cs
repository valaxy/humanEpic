using System;
using System.Globalization;

internal sealed class Int16TypePersistence : ITypePersistence
{
    public bool CanHandle(Type type) => type == typeof(short);
    public object Serialize(object value, Type declaredType) => value;
    public object Deserialize(object rawValue, Type targetType) => Convert.ToInt16(rawValue, CultureInfo.InvariantCulture);
}
