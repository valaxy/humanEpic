using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 拓扑画布模型，负责管理画布尺寸、节点尺寸与布局数据。
/// </summary>
public sealed class TopologyCanvas
{
	// 默认画布宽度。
	private const float defaultWidth = 5000f;
	// 默认画布高度。
	private const float defaultHeight = 3200f;
	// 默认节点宽度。
	private const float defaultNodeWidth = 280f;
	// 默认节点高度。
	private const float defaultNodeHeight = 96f;
	// 默认节点回退位置。
	private static readonly Vector2 defaultFallbackPosition = new(80f, 80f);

	// 画布单例。
	private static readonly TopologyCanvas instance = new();

	/// <summary>
	/// 全局画布单例。
	/// </summary>
	public static TopologyCanvas Instance => instance;

	/// <summary>
	/// 虚拟画布宽度。
	/// </summary>
	public float Width { get; }

	/// <summary>
	/// 虚拟画布高度。
	/// </summary>
	public float Height { get; }

	/// <summary>
	/// 节点宽度。
	/// </summary>
	public float NodeWidth { get; }

	/// <summary>
	/// 节点高度。
	/// </summary>
	public float NodeHeight { get; }

	/// <summary>
	/// 画布尺寸。
	/// </summary>
	public Vector2 CanvasSize => new(Width, Height);

	/// <summary>
	/// 节点尺寸。
	/// </summary>
	public Vector2 NodeSize => new(NodeWidth, NodeHeight);

	/// <summary>
	/// 当前节点定义映射。
	/// </summary>
	public IReadOnlyDictionary<string, MetricNode> NodesByNodeId { get; private set; } =
		new Dictionary<string, MetricNode>(StringComparer.Ordinal);

	/// <summary>
	/// 当前节点布局映射。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> NodeLayoutByNodeId { get; private set; } =
		new Dictionary<string, Vector2>(StringComparer.Ordinal);

	/// <summary>
	/// 当前连线。
	/// </summary>
	public IReadOnlyList<MetricEdge> ActiveEdges { get; private set; } = Array.Empty<MetricEdge>();

	/// <summary>
	/// 当前系统。
	/// </summary>
	public GameSystem GameSystem { get; private set; } = GameSystem.Empty;

	/// <summary>
	/// 当前拓扑。
	/// </summary>
	public Topology CurrentTopology { get; private set; } = new(GameSystem.AllTopologyScopeKey, "全部");

	/// <summary>
	/// 当前作用域键。
	/// </summary>
	public string CurrentScopeKey { get; private set; } = GameSystem.AllTopologyScopeKey;

	/// <summary>
	/// 当前激活节点集合。
	/// </summary>
	public IReadOnlyCollection<string> ActiveNodeIds => activeNodeIds;

	/// <summary>
	/// 当前布局坐标。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> LayoutPositions => layoutPositions;

	/// <summary>
	/// 当前选中节点。
	/// </summary>
	public string SelectedNodeId { get; private set; } = string.Empty;

	// 激活节点集合。
	private HashSet<string> activeNodeIds = new(StringComparer.Ordinal);
	// 布局坐标缓存。
	private IReadOnlyDictionary<string, Vector2> layoutPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);

	/// <summary>
	/// 创建拓扑画布。
	/// </summary>
	private TopologyCanvas(float width = defaultWidth, float height = defaultHeight, float inputNodeWidth = defaultNodeWidth, float inputNodeHeight = defaultNodeHeight)
	{
		Width = Mathf.Max(width, 1f);
		Height = Mathf.Max(height, 1f);
		NodeWidth = Mathf.Max(inputNodeWidth, 1f);
		NodeHeight = Mathf.Max(inputNodeHeight, 1f);
	}

	/// <summary>
	/// 刷新当前节点、连线与布局快照。
	/// </summary>
	public void ApplySnapshot(
		IReadOnlyDictionary<string, MetricNode> nodesByNodeId,
		IReadOnlyDictionary<string, Vector2> layoutByNodeId,
		IReadOnlyList<MetricEdge> activeEdges)
	{
		NodesByNodeId = nodesByNodeId
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		NodeLayoutByNodeId = layoutByNodeId
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		ActiveEdges = activeEdges.ToList();
	}

	/// <summary>
	/// 重新加载指定作用域拓扑并重建快照。
	/// </summary>
	public void Reload(
		GameSystem gameSystem,
		string scopeKey,
		IReadOnlyDictionary<string, Vector2> persistedLayout,
		Vector2? fallbackPosition = null)
	{
		GameSystem = gameSystem;
		CurrentScopeKey = string.IsNullOrWhiteSpace(scopeKey) ? GameSystem.AllTopologyScopeKey : scopeKey;
		CurrentTopology = gameSystem.GetTopology(CurrentScopeKey);

		HashSet<string> validMetricNodeIds = CurrentTopology
			.CollectMetricNodeIds()
			.ToHashSet(StringComparer.Ordinal);

		layoutPositions = persistedLayout
			.Where(pair => validMetricNodeIds.Contains(pair.Key))
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);

		activeNodeIds = gameSystem.DeriveActiveNodeIds(layoutPositions.Keys, CurrentTopology);
		SelectedNodeId = string.Empty;

		(IReadOnlyDictionary<string, MetricNode> nodesByNodeId,
			IReadOnlyDictionary<string, Vector2> layoutByNodeId,
			IReadOnlyList<MetricEdge> activeEdges) = BuildScopeSnapshot(activeNodeIds, layoutPositions, fallbackPosition ?? defaultFallbackPosition);
		ApplySnapshot(nodesByNodeId, layoutByNodeId, activeEdges);
	}

	/// <summary>
	/// 将节点激活到当前拓扑画布。
	/// </summary>
	public bool ActivateNode(string nodeId, Vector2 graphPosition)
	{
		if (CurrentTopology.MetricNodesById.ContainsKey(nodeId) == false)
		{
			return false;
		}

		bool isNewlyActivated = activeNodeIds.Add(nodeId);
		if (isNewlyActivated == false)
		{
			return false;
		}

		Dictionary<string, Vector2> nextLayout = activeNodeIds
			.ToDictionary(
				node => node,
				node => layoutPositions.TryGetValue(node, out Vector2 existingPosition) ? existingPosition : graphPosition,
				StringComparer.Ordinal);
		nextLayout[nodeId] = graphPosition;
		layoutPositions = nextLayout;
		rebuildFromCurrentState();
		return true;
	}

	/// <summary>
	/// 删除当前拓扑中的激活节点。
	/// </summary>
	public bool RemoveNode(string nodeId)
	{
		bool removed = activeNodeIds.Remove(nodeId);
		if (removed == false)
		{
			return false;
		}

		layoutPositions = layoutPositions
			.Where(pair => activeNodeIds.Contains(pair.Key))
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		SelectedNodeId = string.Empty;
		rebuildFromCurrentState();
		return true;
	}

	/// <summary>
	/// 更新节点布局坐标。
	/// </summary>
	public bool TryUpdateNodeLayout(string nodeId, Vector2 position)
	{
		if (activeNodeIds.Contains(nodeId) == false)
		{
			return false;
		}

		Dictionary<string, Vector2> nextLayout = layoutPositions
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		nextLayout[nodeId] = position;
		layoutPositions = nextLayout;
		rebuildFromCurrentState();
		return true;
	}

	/// <summary>
	/// 更新当前选中节点。
	/// </summary>
	public void SelectNode(string nodeId)
	{
		SelectedNodeId = nodeId;
	}

	/// <summary>
	/// 清空当前画布快照。
	/// </summary>
	public void Clear()
	{
		SelectedNodeId = string.Empty;
		ApplySnapshot(
			new Dictionary<string, MetricNode>(StringComparer.Ordinal),
			new Dictionary<string, Vector2>(StringComparer.Ordinal),
			Array.Empty<MetricEdge>());
	}

	/// <summary>
	/// 基于当前拓扑与激活节点重建渲染快照。
	/// </summary>
	public (IReadOnlyDictionary<string, MetricNode> NodesByNodeId, IReadOnlyDictionary<string, Vector2> LayoutByNodeId, IReadOnlyList<MetricEdge> ActiveEdges) BuildScopeSnapshot(
		IReadOnlyCollection<string> currentActiveNodeIds,
		IReadOnlyDictionary<string, Vector2> currentLayoutPositions,
		Vector2 fallbackPosition)
	{
		Dictionary<string, MetricNode> nodesByNodeId = CurrentTopology.MetricNodes
			.Where(metric => currentActiveNodeIds.Contains(metric.NodeId))
			.ToDictionary(metric => metric.NodeId, metric => metric, StringComparer.Ordinal);

		Dictionary<string, Vector2> layoutByNodeId = currentActiveNodeIds
			.ToDictionary(
				nodeId => nodeId,
				nodeId => currentLayoutPositions.TryGetValue(nodeId, out Vector2 savedPosition)
					? savedPosition
					: fallbackPosition,
				StringComparer.Ordinal);

		IReadOnlyList<MetricEdge> activeEdges = CurrentTopology.MetricEdges
			.Where(edge => currentActiveNodeIds.Contains(edge.FromNodeId) && currentActiveNodeIds.Contains(edge.ToNodeId))
			.ToList();

		return (nodesByNodeId, layoutByNodeId, activeEdges);
	}

	/// <summary>
	/// 尝试根据画布坐标拾取节点。
	/// </summary>
	public bool TryPickNodeIdAt(Vector2 canvasPosition, out string nodeId)
	{
		nodeId = NodeLayoutByNodeId
			.Where(pair => new Rect2(pair.Value, NodeSize).HasPoint(canvasPosition))
			.Select(static pair => pair.Key)
			.FirstOrDefault() ?? string.Empty;
		return string.IsNullOrWhiteSpace(nodeId) == false;
	}

	// 根据当前缓存状态重建可渲染快照。
	private void rebuildFromCurrentState()
	{
		(IReadOnlyDictionary<string, MetricNode> nodesByNodeId,
			IReadOnlyDictionary<string, Vector2> layoutByNodeId,
			IReadOnlyList<MetricEdge> activeEdges) = BuildScopeSnapshot(activeNodeIds, layoutPositions, defaultFallbackPosition);
		ApplySnapshot(nodesByNodeId, layoutByNodeId, activeEdges);
	}
}
