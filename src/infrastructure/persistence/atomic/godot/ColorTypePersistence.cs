using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        Dictionary<string, object> data = ColorTypePersistence.readGodotNode(rawValue, nameof(Color));
        float r = Convert.ToSingle(data["r"], CultureInfo.InvariantCulture);
        float g = Convert.ToSingle(data["g"], CultureInfo.InvariantCulture);
        float b = Convert.ToSingle(data["b"], CultureInfo.InvariantCulture);
        float a = Convert.ToSingle(data["a"], CultureInfo.InvariantCulture);
        return new Color(r, g, b, a);
    }


    internal static Dictionary<string, object> readGodotNode(object rawValue, string targetTypeName)
    {
        if (rawValue is not Dictionary<string, object> node)
        {
            throw new InvalidOperationException($"Godot 值类型数据结构非法: {targetTypeName}");
        }

        if (!node.ContainsKey("__godot") || !node.ContainsKey("type"))
        {
            throw new InvalidOperationException($"Godot 值类型缺少必要键: {targetTypeName}");
        }

        if (node["type"].ToString() != targetTypeName)
        {
            throw new InvalidOperationException($"Godot 值类型不匹配: 期望 {targetTypeName}");
        }

        return node
            .Where(pair => pair.Key != "__godot" && pair.Key != "type")
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
