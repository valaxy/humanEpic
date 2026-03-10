using System;
using System.Globalization;

internal sealed class EnumTypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => type.IsEnum;

	public object Serialize(object value, Type declaredType)
	{
		return Convert.ToInt32(value, CultureInfo.InvariantCulture);
	}

	public object Deserialize(object rawValue, Type targetType)
	{
		int enumId = Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
		return Enum.ToObject(targetType, enumId);
	}
}
