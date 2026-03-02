using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class JsonUtility
{
    // 从默认存档路径加载原生 C# 字典数据。
    public static Dictionary<string, object> LoadDataFromJsonFile(string savePath)
    {
        if (!FileAccess.FileExists(savePath))
        {
            throw new InvalidOperationException($"Save file not found at {savePath}");
        }

        using FileAccess file = FileAccess.Open(savePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"Failed to open save file at {savePath}: {FileAccess.GetOpenError()}");
        }

        string jsonText = file.GetAsText();
        using JsonDocument doc = JsonDocument.Parse(jsonText);
        return (Dictionary<string, object>)toNativeCsharp(doc.RootElement)!;
    }


    // JsonElement 转换为原生 C# 对象。
    private static object? toNativeCsharp(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return element
                    .EnumerateObject()
                    .ToDictionary(prop => prop.Name, prop => toNativeCsharp(prop.Value)!);
            case JsonValueKind.Array:
                return element
                    .EnumerateArray()
                    .Select(item => toNativeCsharp(item)!)
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
                return null;
        }
    }
}