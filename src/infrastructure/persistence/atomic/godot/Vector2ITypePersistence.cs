using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

internal sealed class Vector2ITypePersistence : ITypePersistence
{
	public bool CanHandle(Type type) => type == typeof(Vector2I);

	public object Serialize(object value, Type declaredType)
	{
		Vector2I vector = (Vector2I)value;
		return new Dictionary<string, object>
		{
			{ "__godot", true },
			{ "type", nameof(Vector2I) },
			{ "x", vector.X },
			{ "y", vector.Y }
		};
	}

	public object Deserialize(object rawValue, Type targetType)
	{
		Dictionary<string, object> data = DomainModelJsonPersistence.readGodotNode(rawValue, nameof(Vector2I));
		int x = Convert.ToInt32(data["x"], CultureInfo.InvariantCulture);
		int y = Convert.ToInt32(data["y"], CultureInfo.InvariantCulture);
		return new Vector2I(x, y);
	}
}
