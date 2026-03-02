using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public static class JsonUtility
{
    // 从默认存档路径加载原生 C# 字典数据。
    public static Dictionary<string, object> LoadDataFromJsonFile(string savePath)
    {
        // 转换 Godot res:// 路径为绝对路径
        string absolutePath = ProjectSettings.GlobalizePath(savePath);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Save file not found at {absolutePath}");
        }

        // 报错直接就失败
        string jsonText = File.ReadAllText(absolutePath);
        using JsonDocument doc = JsonDocument.Parse(jsonText);
        return (Dictionary<string, object>)toNativeCsharp(doc.RootElement)!;

    }


    // JsonElement 转换为原生 C# 对象。
    private static object? toNativeCsharp(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                Dictionary<string, object> dict = new();
                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    dict[prop.Name] = toNativeCsharp(prop.Value)!;
                }
                return dict;
            case JsonValueKind.Array:
                List<object> list = new();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    list.Add(toNativeCsharp(item)!);
                }
                return list;
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