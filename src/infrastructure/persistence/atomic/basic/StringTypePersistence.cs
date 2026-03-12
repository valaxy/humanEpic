using System;

internal sealed class StringTypePersistence : ITypePersistence
{
    public bool CanHandle(Type type) => type == typeof(string);
    public object Serialize(object value, Type declaredType) => value;
    public object Deserialize(object rawValue, Type targetType)
    {
        return rawValue.ToString() ?? throw new InvalidOperationException("字符串转换失败");
    }
}
