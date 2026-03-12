using System;
using System.Globalization;

internal sealed class BoolTypePersistence : ITypePersistence
{
    public bool CanHandle(Type type) => type == typeof(bool);
    public object Serialize(object value, Type declaredType) => value;
    public object Deserialize(object rawValue, Type targetType) => Convert.ToBoolean(rawValue, CultureInfo.InvariantCulture);
}
