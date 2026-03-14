using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 拓扑画布模型，负责管理画布尺寸、节点尺寸与布局数据。
/// 一次性只能管理一个MetricScope的内容
/// </summary>
public sealed class TopologyCanvas
{
	// 画布单例。
	private static readonly TopologyCanvas instance = new();

	/// <summary>
	/// 全局画布单例。
	/// </summary>
	public static TopologyCanvas Instance => instance;




	// 默认画布宽度。
	private const float defaultWidth = 5000f;
	// 默认画布高度。
	private const float defaultHeight = 5000f;

	// 默认节点宽度。
	private const float defaultNodeWidth = 280f;
	// 默认节点高度。
	private const float defaultNodeHeight = 96f;

	// 当前拓扑
	private MetricScope scope = null!;

	// 节点集合
	private Dictionary<string, TopologyNode> nodes = null!;

	// 边集合
	private List<TopologyEdge> edges = null!;

	/// <summary>
	/// 虚拟画布宽度。
	/// </summary>
	public float CanvasWidth => defaultWidth;

	/// <summary>
	/// 虚拟画布高度。
	/// </summary>
	public float CanvasHeight => defaultHeight;

	/// <summary>
	/// 节点宽度。
	/// </summary>
	public float NodeWidth => defaultNodeWidth;

	/// <summary>
	/// 节点高度。
	/// </summary>
	public float NodeHeight => defaultNodeHeight;

	/// <summary>
	/// 画布尺寸。
	/// </summary>
	public Vector2 CanvasSize => new(CanvasWidth, CanvasHeight);

	/// <summary>
	/// 节点尺寸。
	/// </summary>
	public Vector2 NodeSize => new(NodeWidth, NodeHeight);


	/// <summary>
	/// 节点的数据
	/// </summary>
	public IReadOnlyDictionary<string, TopologyNode> Nodes => nodes;

	/// <summary>
	/// 边的数据（边的数据直接复用MetricRelation好了，反正应该没差太多）
	/// </summary>
	public IReadOnlyList<TopologyEdge> Edges => edges;





	/// <summary>
	/// 创建拓扑画布。
	/// </summary>
	public TopologyCanvas() { }


	/// <summary>
	/// 初始化操作，重新加载指定作用域并重建快照。
	/// </summary>
	public void Reload(MetricScope scope)
	{
		this.scope = scope;
		nodes = scope.Metrics.Values
			.Select(metric => new TopologyNode(metric))
			.ToDictionary(node => node.Id, node => node);
		edges = scope.MetricRelations
			.Select(relation => new TopologyEdge(nodes[relation.Input.Name], nodes[relation.Output.Name]))
			.ToList();
		TopologyCanvasLayout.LoadAndApply(scope.Name, this);
	}

	/// <summary>
	/// 反激活指定节点
	/// </summary>
	public void DeactiveNode(string nodeId)
	{
		if (nodes.ContainsKey(nodeId) == false)
		{
			return;
		}

		nodes[nodeId].IsActive = false;
		TopologyCanvasLayout.Save(scope.Name, this);
	}

	/// <summary>
	/// 更新节点布局坐标。
	/// </summary>
	public void UpdateNodePosition(string nodeId, Vector2 position)
	{
		if (nodes.ContainsKey(nodeId) == false)
		{
			return;
		}

		nodes[nodeId].Position = position;
		TopologyCanvasLayout.Save(scope.Name, this);
	}



	/// <summary>
	/// 尝试根据画布坐标拾取节点。
	/// </summary>
	public bool TryPickNodeIdAt(Vector2 canvasPosition, out string nodeId)
	{
		nodeId = nodes
			.Where(node => node.Value.IsActive)
			.Where(node =>
			{
				// 当然这里的拾取算法可以优化，不过数据量不大没关系
				var nodePos = node.Value.Position;
				var nodeRect = new Rect2(nodePos, NodeSize);
				return nodeRect.HasPoint(canvasPosition);
			})
			.Select(static pair => pair.Key)
			.FirstOrDefault() ?? string.Empty;
		return string.IsNullOrWhiteSpace(nodeId) == false;
	}
}
