using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// flowtool 位置布局持久化。
/// </summary>
public sealed class FlowToolLayoutStore
{
	// 布局存档路径。
	private readonly string layoutFilePath;
	// JSON 序列化选项。
	private readonly JsonSerializerOptions jsonSerializerOptions;

	/// <summary>
	/// 构造布局存储器。
	/// </summary>
	public FlowToolLayoutStore(string userPath = "user://flowtool_layout.json")
	{
		layoutFilePath = ProjectSettings.GlobalizePath(userPath);
		jsonSerializerOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			Converters = { new JsonStringEnumConverter() }
		};
	}

	/// <summary>
	/// 读取布局字典。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> Load()
	{
		if (File.Exists(layoutFilePath) == false)
		{
			return new Dictionary<string, Vector2>();
		}

		string jsonText = File.ReadAllText(layoutFilePath);
		FlowToolLayoutFile layoutFile = JsonSerializer.Deserialize<FlowToolLayoutFile>(jsonText, jsonSerializerOptions) ?? new FlowToolLayoutFile();
		return layoutFile.Nodes
			.ToDictionary(
				static pair => pair.Key,
				static pair => new Vector2(pair.Value.X, pair.Value.Y),
				StringComparer.Ordinal
			);
	}

	/// <summary>
	/// 保存布局字典。
	/// </summary>
	public void Save(IReadOnlyDictionary<string, Vector2> nodePositions)
	{
		FlowToolLayoutFile layoutFile = new()
		{
			Nodes = nodePositions.ToDictionary(
				static pair => pair.Key,
				static pair => new FlowToolVector2Dto { X = pair.Value.X, Y = pair.Value.Y },
				StringComparer.Ordinal
			)
		};

		string outputDirectoryPath = Path.GetDirectoryName(layoutFilePath) ?? string.Empty;
		if (string.IsNullOrWhiteSpace(outputDirectoryPath) == false)
		{
			Directory.CreateDirectory(outputDirectoryPath);
		}

		string jsonText = JsonSerializer.Serialize(layoutFile, jsonSerializerOptions);
		File.WriteAllText(layoutFilePath, jsonText);
	}

	/// <summary>
	/// 清理失效节点布局并返回有效布局。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> FilterInvalidNodes(IReadOnlyDictionary<string, Vector2> nodePositions, IReadOnlyCollection<string> validNodeIds)
	{
		HashSet<string> validNodeIdSet = validNodeIds.ToHashSet(StringComparer.Ordinal);
		return nodePositions
			.Where(pair => validNodeIdSet.Contains(pair.Key))
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
	}

	// 布局 JSON 文件实体。
	private sealed class FlowToolLayoutFile
	{
		// 节点位置映射。
		public Dictionary<string, FlowToolVector2Dto> Nodes { get; set; } = new(StringComparer.Ordinal);
	}

	// 二维向量 DTO。
	private sealed class FlowToolVector2Dto
	{
		// X 坐标。
		public float X { get; set; }
		// Y 坐标。
		public float Y { get; set; }
	}
}
