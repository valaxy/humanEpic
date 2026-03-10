using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

internal sealed class ColorTypePersistence : ITypePersistence
{
	public bool CanHandle(Type type) => type == typeof(Color);

	public object Serialize(object value, Type declaredType)
	{
		Color color = (Color)value;
		return new Dictionary<string, object>
		{
			{ "__godot", true },
			{ "type", nameof(Color) },
			{ "r", color.R },
			{ "g", color.G },
			{ "b", color.B },
			{ "a", color.A }
		};
	}

	public object Deserialize(object rawValue, Type targetType)
	{
		Dictionary<string, object> data = DomainModelJsonPersistence.readGodotNode(rawValue, nameof(Color));
		float r = Convert.ToSingle(data["r"], CultureInfo.InvariantCulture);
		float g = Convert.ToSingle(data["g"], CultureInfo.InvariantCulture);
		float b = Convert.ToSingle(data["b"], CultureInfo.InvariantCulture);
		float a = Convert.ToSingle(data["a"], CultureInfo.InvariantCulture);
		return new Color(r, g, b, a);
	}
}
