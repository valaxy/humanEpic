using System;
using System.Globalization;

internal sealed class UInt32TypePersistence : ITypePersistence
{
    public bool CanHandle(Type type) => type == typeof(uint);
    public object Serialize(object value, Type declaredType) => value;
    public object Deserialize(object rawValue, Type targetType) => Convert.ToUInt32(rawValue, CultureInfo.InvariantCulture);
}
