using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class JsonUtility
{
    /// <summary>
    /// 记载JSON文件为原生C#字典数据。采用godot API，这样支持res://路径
    /// </summary>
    public static Dictionary<string, object> LoadDataFromJsonFile(string jsonFilePath)
    {
        if (!FileAccess.FileExists(jsonFilePath))
        {
            throw new InvalidOperationException($"Save file not found at {jsonFilePath}");
        }

        using FileAccess file = FileAccess.Open(jsonFilePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"Failed to open save file at {jsonFilePath}: {FileAccess.GetOpenError()}");
        }

        string jsonText = file.GetAsText();
        using JsonDocument doc = JsonDocument.Parse(jsonText);
        object? nativeObject = ToNativeObject(doc.RootElement);
        return nativeObject as Dictionary<string, object>
            ?? throw new InvalidOperationException("JSON 根节点必须为对象");
    }


    // JsonElement 转换为原生 C# 对象。
    public static object? ToNativeObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return element
                    .EnumerateObject()
                    .ToDictionary(prop => prop.Name, prop => ToNativeObject(prop.Value)!);
            case JsonValueKind.Array:
                return element
                    .EnumerateArray()
                    .Select(item => ToNativeObject(item)!)
                    .ToList();
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out long l))
                {
                    return l;
                }

                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            default:
                throw new InvalidOperationException($"不支持的 JSON 节点类型: {element.ValueKind}");
        }
    }
}