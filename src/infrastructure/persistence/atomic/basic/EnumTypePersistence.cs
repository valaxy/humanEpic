using System;
using System.Globalization;

internal sealed class EnumTypePersistence : ITypePersistence
{
    public bool CanHandle(Type type) => type.IsEnum;

    // 重点是将枚举值转换为整数
    public object Serialize(object value, Type declaredType) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    public object Deserialize(object rawValue, Type targetType)
    {
        int enumId = Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
        return Enum.ToObject(targetType, enumId);
    }
}
