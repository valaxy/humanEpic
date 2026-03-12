using System;
using System.Globalization;

internal sealed class Int64TypePersistence : ITypePersistence
{
    public bool CanHandle(Type type) => type == typeof(long);
    public object Serialize(object value, Type declaredType) => value;
    public object Deserialize(object rawValue, Type targetType) => Convert.ToInt64(rawValue, CultureInfo.InvariantCulture);
}
