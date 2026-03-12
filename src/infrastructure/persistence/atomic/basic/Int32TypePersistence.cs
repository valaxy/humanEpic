using System;
using System.Globalization;

internal sealed class Int32TypePersistence : ITypePersistence
{
    public bool CanHandle(Type type) => type == typeof(int);
    public object Serialize(object value, Type declaredType) => value;
    public object Deserialize(object rawValue, Type targetType) => Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
}
