using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

internal sealed class Vector2TypePersistence : IAtomicTypePersistence
{
	public bool CanHandle(Type type) => type == typeof(Vector2);

	public object Serialize(object value, Type declaredType)
	{
		Vector2 vector = (Vector2)value;
		return new Dictionary<string, object>
		{
			{ "__godot", true },
			{ "type", nameof(Vector2) },
			{ "x", vector.X },
			{ "y", vector.Y }
		};
	}

	public object Deserialize(object rawValue, Type targetType)
	{
		Dictionary<string, object> data = DomainModelJsonPersistence.readGodotNode(rawValue, nameof(Vector2));
		float x = Convert.ToSingle(data["x"], CultureInfo.InvariantCulture);
		float y = Convert.ToSingle(data["y"], CultureInfo.InvariantCulture);
		return new Vector2(x, y);
	}
}
