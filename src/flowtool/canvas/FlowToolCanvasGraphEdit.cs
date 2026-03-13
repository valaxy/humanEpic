using Godot;
using GodotDictionary = Godot.Collections.Dictionary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// 左侧画布 GraphEdit，负责接受拖拽激活。
/// </summary>
public partial class FlowToolCanvasGraphEdit : GraphEdit
{
	// 影子节点标题前缀。
	// 删除节点回调。
	private Action<string> deleteNodeRequested = static _ => { };
	// 当前拖拽影子节点。
	private GraphNode? dropShadowNode;
	// 当前影子节点对应 ID。
	private string dropShadowNodeId = string.Empty;
	// 当前画布节点映射。
	private Dictionary<string, GraphNode> activeGraphNodes = new(StringComparer.Ordinal);
	// 当前画布节点名映射。
	private Dictionary<string, string> graphNodeNameByNodeId = new(StringComparer.Ordinal);
	// 当前节点删除按钮映射。
	private Dictionary<string, Button> deleteButtonByNodeId = new(StringComparer.Ordinal);

	/// <summary>
	/// 当前是否已有已渲染节点。
	/// </summary>
	public bool HasRenderedNodes => activeGraphNodes.Count > 0;

	/// <summary>
	/// 节点拖入画布时触发。
	/// </summary>
	[Signal]
	public delegate void NodePayloadDroppedEventHandler(string nodeId, Vector2 graphPosition);

	/// <summary>
	/// 绑定删除节点回调。
	/// </summary>
	public void SetDeleteNodeRequested(Action<string> deleteNodeRequested)
	{
		this.deleteNodeRequested = deleteNodeRequested;
	}

	/// <summary>
	/// 渲染当前作用域下的节点与连线。
	/// </summary>
	public void RenderTopology(FlowToolTopology topology, IReadOnlyCollection<string> activeNodeIds, IReadOnlyDictionary<string, Vector2> layoutPositions)
	{
		activeGraphNodes.Values.ToList().ForEach(static node => node.QueueFree());
		activeGraphNodes = new Dictionary<string, GraphNode>(StringComparer.Ordinal);
		graphNodeNameByNodeId = new Dictionary<string, string>(StringComparer.Ordinal);
		deleteButtonByNodeId = new Dictionary<string, Button>(StringComparer.Ordinal);

		IReadOnlyList<FlowToolMetricNode> activeMetrics = topology.Metrics
			.Where(metric => activeNodeIds.Contains(metric.NodeId))
			.OrderBy(static metric => metric.DisplayName, StringComparer.Ordinal)
			.ToList();

		activeMetrics
			.Select(metric => createGraphNode(metric, layoutPositions))
			.ToList()
			.ForEach(node => AddChild(node));

		ClearConnections();
		topology.Edges
			.Where(edge => activeNodeIds.Contains(edge.FromNodeId) && activeNodeIds.Contains(edge.ToNodeId))
			.ToList()
			.ForEach(connectEdgeIfPossible);
	}

	/// <summary>
	/// 采集当前画布布局坐标。
	/// </summary>
	public IReadOnlyDictionary<string, Vector2> CollectCurrentLayout()
	{
		return activeGraphNodes
			.ToDictionary(static pair => pair.Key, static pair => pair.Value.PositionOffset, StringComparer.Ordinal);
	}

	/// <summary>
	/// 根据当前选中状态切换删除按钮可见性。
	/// </summary>
	public void UpdateDeleteButtonVisibility()
	{
		deleteButtonByNodeId
			.ToList()
			.ForEach(pair => pair.Value.Visible = activeGraphNodes.TryGetValue(pair.Key, out GraphNode? graphNode) && graphNode.Selected);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationDragEnd)
		{
			clearDropShadow();
		}
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType != Variant.Type.Dictionary)
		{
			clearDropShadow();
			return false;
		}

		GodotDictionary payload = data.AsGodotDictionary();
		bool canDrop = payload.ContainsKey("nodeId");
		if (canDrop == false)
		{
			clearDropShadow();
			return false;
		}

		string nodeId = payload["nodeId"].AsString();
		string displayText = payload.ContainsKey("displayText") ? payload["displayText"].AsString() : nodeId;
		Vector2 graphPosition = toGraphPosition(atPosition);
		showOrMoveDropShadow(nodeId, displayText, graphPosition);
		return true;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		GodotDictionary payload = data.AsGodotDictionary();
		string nodeId = payload["nodeId"].AsString();
		Vector2 graphPosition = toGraphPosition(atPosition);
		clearDropShadow();
		EmitSignal(SignalName.NodePayloadDropped, nodeId, graphPosition);
	}

	// 将鼠标坐标转换为 GraphEdit 画布坐标，确保预览与落点一致。
	private Vector2 toGraphPosition(Vector2 atPosition)
	{
		float safeZoom = Mathf.IsZeroApprox(Zoom) ? 1f : Zoom;
		return (atPosition / safeZoom) + ScrollOffset;
	}

	// 显示或移动拖拽影子节点。
	private void showOrMoveDropShadow(string nodeId, string displayText, Vector2 graphPosition)
	{
		if (dropShadowNode is null || dropShadowNodeId != nodeId)
		{
			clearDropShadow();
			dropShadowNode = GraphNodeFactory.CreateDropShadowNode(nodeId, displayText);
			dropShadowNodeId = nodeId;
			AddChild(dropShadowNode);
		}

		dropShadowNode.PositionOffset = graphPosition;
	}

	// 清理拖拽影子节点。
	private void clearDropShadow()
	{
		dropShadowNode?.QueueFree();
		dropShadowNode = null;
		dropShadowNodeId = string.Empty;
	}

	// 创建 GraphNode。
	private GraphNode createGraphNode(FlowToolMetricNode metricNode, IReadOnlyDictionary<string, Vector2> layoutPositions)
	{
		string nodeName = $"Node_{activeGraphNodes.Count.ToString(CultureInfo.InvariantCulture)}";
		graphNodeNameByNodeId[metricNode.NodeId] = nodeName;

		FlowToolGraphNodeBuildResult buildResult = GraphNodeFactory.CreateMetricNode(
			nodeName,
			metricNode.NodeId,
			metricNode.DisplayName,
			$"指标: {metricNode.MetricName} | 类型: {metricNode.TypeDisplayName}",
			layoutPositions.TryGetValue(metricNode.NodeId, out Vector2 position) ? position : new Vector2(80f, 80f),
			() => deleteNodeRequested(metricNode.NodeId));

		deleteButtonByNodeId[buildResult.NodeId] = buildResult.DeleteButton;
		activeGraphNodes[buildResult.NodeId] = buildResult.GraphNode;
		return buildResult.GraphNode;
	}

	// 建立自动连线。
	private void connectEdgeIfPossible(FlowToolEdge edge)
	{
		bool hasFromNode = graphNodeNameByNodeId.TryGetValue(edge.FromNodeId, out string? fromNodeName);
		bool hasToNode = graphNodeNameByNodeId.TryGetValue(edge.ToNodeId, out string? toNodeName);
		if (hasFromNode == false || hasToNode == false)
		{
			return;
		}

		if (IsNodeConnected(fromNodeName!, 0, toNodeName!, 0))
		{
			return;
		}

		ConnectNode(fromNodeName!, 0, toNodeName!, 0);
	}

}
