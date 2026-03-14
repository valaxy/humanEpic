using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 工具类，负责TopologyCanvas的持久化。
/// </summary>
public static class TopologyCanvasLayout
{
	// 默认节点位置数据。
	private sealed class NodeLayoutData
	{
		public float X { get; set; }
		public float Y { get; set; }
		public bool IsActive { get; set; }
	}

	// 作用域布局数据。
	private sealed class ScopeLayoutData
	{
		// 节点布局映射。
		public Dictionary<string, NodeLayoutData> Nodes { get; set; } = new(StringComparer.Ordinal);
	}

	// 布局文件根节点。
	private sealed class LayoutRootData
	{
		// 作用域布局集合。
		public Dictionary<string, ScopeLayoutData> Scopes { get; set; } = new(StringComparer.Ordinal);
	}




	// 默认布局存档路径。
	private const string defaultLayoutFilePath = "res://config/flowtool_layout.json";

	// JSON 序列化选项。
	private static readonly JsonSerializerOptions jsonSerializerOptions;

	/// <summary>
	/// 构造布局存储器。
	/// </summary>
	static TopologyCanvasLayout()
	{
		jsonSerializerOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			Converters = { new JsonStringEnumConverter() }
		};
	}




	/// <summary>
	/// 读取指定作用域的布局字典并应用到模型里
	/// </summary>
	public static void LoadAndApply(string scopeName, TopologyCanvas topologyCanvas)
	{
		// AI这写的什么垃圾逻辑，懒得改能用就行了
		Debug.Assert(!string.IsNullOrWhiteSpace(scopeName));

		LayoutRootData root = loadRootData();
		Dictionary<string, NodeLayoutData> persistedNodeLayout = root.Scopes
			.TryGetValue(scopeName, out ScopeLayoutData? scopeLayout)
			? scopeLayout.Nodes
			: new Dictionary<string, NodeLayoutData>(StringComparer.Ordinal);

		Dictionary<string, NodeLayoutData> defaultNodeLayout = topologyCanvas.Nodes
			.Values
			.OrderBy(node => node.Id, StringComparer.Ordinal)
			.Select((node, index) => new
			{
				NodeId = node.Id,
				Layout = new NodeLayoutData
				{
					X = 120f + ((index % 4) * 360f), // 按照一排4个节点的方式放置
					Y = 160f + ((index / 4) * 220f),
					IsActive = node.IsActive
				}
			})
			.ToDictionary(item => item.NodeId, item => item.Layout, StringComparer.Ordinal);

		Dictionary<string, NodeLayoutData> resolvedNodeLayout = defaultNodeLayout
			.ToDictionary(
				pair => pair.Key,
				pair => persistedNodeLayout.TryGetValue(pair.Key, out NodeLayoutData? persistedLayout)
					? persistedLayout
					: pair.Value,
				StringComparer.Ordinal);

		topologyCanvas.Nodes
			.Values
			.ToList()
			.ForEach(node =>
			{
				NodeLayoutData nodeLayout = resolvedNodeLayout[node.Id];
				node.Position = new Vector2(nodeLayout.X, nodeLayout.Y);
				node.IsActive = nodeLayout.IsActive;
			});
	}

	/// <summary>
	/// 保存指定作用域的布局数据
	/// </summary>
	public static void Save(string scopeName, TopologyCanvas topologyCanvas)
	{
		Debug.Assert(!string.IsNullOrWhiteSpace(scopeName));

		LayoutRootData root = loadRootData();
		Dictionary<string, NodeLayoutData> nodeLayout = topologyCanvas.Nodes
			.Values
			.ToDictionary(
				node => node.Id,
				node => new NodeLayoutData
				{
					X = node.Position.X,
					Y = node.Position.Y,
					IsActive = node.IsActive
				},
				StringComparer.Ordinal);
		root.Scopes[scopeName] = new ScopeLayoutData
		{
			Nodes = nodeLayout
		};

		string globalPath = ProjectSettings.GlobalizePath(defaultLayoutFilePath);
		string? directoryPath = Path.GetDirectoryName(globalPath);
		if (!string.IsNullOrWhiteSpace(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}

		string json = JsonSerializer.Serialize(root, jsonSerializerOptions);
		File.WriteAllText(globalPath, json);
	}

	// 读取布局文件根节点。
	private static LayoutRootData loadRootData()
	{
		string globalPath = ProjectSettings.GlobalizePath(defaultLayoutFilePath);
		if (File.Exists(globalPath) == false)
		{
			return new LayoutRootData();
		}

		string json = File.ReadAllText(globalPath);
		if (string.IsNullOrWhiteSpace(json))
		{
			return new LayoutRootData();
		}

		return JsonSerializer.Deserialize<LayoutRootData>(json, jsonSerializerOptions) ?? new LayoutRootData();
	}
}
