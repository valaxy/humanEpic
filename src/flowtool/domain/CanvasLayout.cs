using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// canvas 位置布局持久化。
/// </summary>
public sealed class CanvasLayout
{
	// 默认布局存档路径。
	private const string defaultLayoutFilePath = "res://config/flowtool_layout.json";
	// 全部布局作用域键。
	private const string allLayoutScopeKey = "all";

	// 布局存档路径。
	private readonly string layoutFilePath;
	// 当前布局作用域键。
	private readonly string layoutScopeKey;
	// JSON 序列化选项。
	private readonly JsonSerializerOptions jsonSerializerOptions;

	/// <summary>
	/// 构造布局存储器。
	/// </summary>
	public CanvasLayout(string layoutScopeKey, string userPath = defaultLayoutFilePath)
	{
		layoutFilePath = ProjectSettings.GlobalizePath(userPath);
		this.layoutScopeKey = string.IsNullOrWhiteSpace(layoutScopeKey) ? allLayoutScopeKey : layoutScopeKey;
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
		FlowToolLayoutCollectionFile layoutCollectionFile = loadLayoutCollectionFile();
		if (layoutCollectionFile.Scopes.TryGetValue(layoutScopeKey, out FlowToolLayoutFile? scopedLayoutFile))
		{
			return toNodePositions(scopedLayoutFile);
		}

		return new Dictionary<string, Vector2>(StringComparer.Ordinal);
	}

	/// <summary>
	/// 保存布局字典。
	/// </summary>
	public void Save(IReadOnlyDictionary<string, Vector2> nodePositions)
	{
		FlowToolLayoutCollectionFile layoutCollectionFile = loadLayoutCollectionFile();
		layoutCollectionFile.Scopes[layoutScopeKey] = new FlowToolLayoutFile
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

		string jsonText = JsonSerializer.Serialize(layoutCollectionFile, jsonSerializerOptions);
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

	// 读取聚合布局文件。
	private FlowToolLayoutCollectionFile loadLayoutCollectionFile()
	{
		if (File.Exists(layoutFilePath) == false)
		{
			return new FlowToolLayoutCollectionFile();
		}

		string jsonText = File.ReadAllText(layoutFilePath);
		FlowToolLayoutCollectionFile? collectionFile = JsonSerializer.Deserialize<FlowToolLayoutCollectionFile>(jsonText, jsonSerializerOptions);
		if (collectionFile is not null && collectionFile.Scopes.Count > 0)
		{
			return normalizeCollectionFile(collectionFile);
		}

		return new FlowToolLayoutCollectionFile();
	}

	// 统一布局集合实例中的字典比较器与空值状态。
	private static FlowToolLayoutCollectionFile normalizeCollectionFile(FlowToolLayoutCollectionFile collectionFile)
	{
		return new FlowToolLayoutCollectionFile
		{
			Scopes = collectionFile.Scopes
				.ToDictionary(
					static pair => pair.Key,
					static pair => pair.Value ?? new FlowToolLayoutFile(),
					StringComparer.Ordinal
				)
		};
	}

	// 将布局 DTO 转为运行时坐标字典。
	private static IReadOnlyDictionary<string, Vector2> toNodePositions(FlowToolLayoutFile layoutFile)
	{
		return layoutFile.Nodes
			.ToDictionary(
				static pair => pair.Key,
				static pair => new Vector2(pair.Value.X, pair.Value.Y),
				StringComparer.Ordinal
			);
	}

	// 多作用域布局 JSON 文件实体。
	private sealed class FlowToolLayoutCollectionFile
	{
		// 作用域布局映射。
		public Dictionary<string, FlowToolLayoutFile> Scopes { get; set; } = new(StringComparer.Ordinal);
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
