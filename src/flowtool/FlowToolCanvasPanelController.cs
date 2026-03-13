using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// flowtool 中央编辑画布组件。
/// </summary>
public sealed class FlowToolCanvasPanelController
{
	// 指标节点类型标识。
	private const string metricNodeKind = "metric";

	// 画布控件。
	private readonly FlowToolCanvasGraphEdit canvas;
	// 删除节点回调。
	private readonly Action<string> deleteNodeRequested;
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
	/// 构造编辑画布组件。
	/// </summary>
	public FlowToolCanvasPanelController(FlowToolCanvasGraphEdit canvas, Action<string> deleteNodeRequested)
	{
		this.canvas = canvas;
		this.deleteNodeRequested = deleteNodeRequested;
	}

	/// <summary>
	/// 初始化画布交互配置。
	/// </summary>
	public void Configure(Action<string, string, Vector2> onNodePayloadDropped)
	{
		canvas.NodePayloadDropped += (nodeId, nodeKind, graphPosition) => onNodePayloadDropped(nodeId, nodeKind, graphPosition);
		canvas.RightDisconnects = false;
		canvas.ShowZoomLabel = true;
		canvas.Zoom = 1f;
		canvas.MinimapEnabled = true;
		canvas.ShowArrangeButton = false;
	}

	/// <summary>
	/// 渲染当前作用域下的节点与连线。
	/// </summary>
	public void Render(FlowToolTopology topology, IReadOnlyCollection<string> activeNodeIds, IReadOnlyDictionary<string, Vector2> layoutPositions)
	{
		activeGraphNodes.Values.ToList().ForEach(static node => node.QueueFree());
		activeGraphNodes = new Dictionary<string, GraphNode>(StringComparer.Ordinal);
		graphNodeNameByNodeId = new Dictionary<string, string>(StringComparer.Ordinal);
		deleteButtonByNodeId = new Dictionary<string, Button>(StringComparer.Ordinal);

		IReadOnlyDictionary<string, FlowToolMetricNode> metricById = topology.Metrics.ToDictionary(static metric => metric.NodeId, StringComparer.Ordinal);

		IReadOnlyList<FlowToolVisualNodeDescriptor> descriptors = activeNodeIds
			.Select(nodeId => createVisualDescriptor(nodeId, metricById))
			.Where(static descriptor => descriptor is not null)
			.Select(static descriptor => descriptor!)
			.OrderBy(static descriptor => descriptor.Kind, StringComparer.Ordinal)
			.ThenBy(static descriptor => descriptor.DisplayName, StringComparer.Ordinal)
			.ToList();

		descriptors
			.Select(descriptor => createGraphNode(descriptor, layoutPositions))
			.ToList()
			.ForEach(node => canvas.AddChild(node));

		canvas.ClearConnections();
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

	// 构建可视节点描述。
	private static FlowToolVisualNodeDescriptor? createVisualDescriptor(
		string nodeId,
		IReadOnlyDictionary<string, FlowToolMetricNode> metricById)
	{
		if (metricById.TryGetValue(nodeId, out FlowToolMetricNode? metricNode))
		{
			return new FlowToolVisualNodeDescriptor(metricNode.NodeId, metricNode.DisplayName, metricNodeKind, $"指标: {metricNode.MetricName} | 类型: {metricNode.TypeDisplayName}");
		}

		return null;
	}

	// 创建 GraphNode。
	private GraphNode createGraphNode(FlowToolVisualNodeDescriptor descriptor, IReadOnlyDictionary<string, Vector2> layoutPositions)
	{
		string nodeName = $"Node_{activeGraphNodes.Count.ToString(CultureInfo.InvariantCulture)}";
		graphNodeNameByNodeId[descriptor.NodeId] = nodeName;

		GraphNode graphNode = new()
		{
			Name = nodeName,
			Title = string.Empty,
			PositionOffset = layoutPositions.TryGetValue(descriptor.NodeId, out Vector2 position) ? position : new Vector2(80f, 80f),
			Resizable = false,
			Draggable = true,
			CustomMinimumSize = new Vector2(240f, 0f)
		};
		graphNode.GetTitlebarHBox().Visible = false;

		VBoxContainer body = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		HBoxContainer header = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		Label titleLabel = new()
		{
			Text = descriptor.DisplayName,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};
		Button deleteButton = createDeleteButton(descriptor.NodeId);
		Label kindLabel = new()
		{
			Text = descriptor.DetailText,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		titleLabel.SizeFlagsStretchRatio = 1f;
		header.AddChild(titleLabel);
		header.AddChild(deleteButton);
		body.AddChild(header);
		body.AddChild(kindLabel);
		graphNode.AddChild(body);

		Color portColor = new Color(0.39f, 0.69f, 0.92f);
		graphNode.SetSlot(0, true, 0, portColor, true, 0, portColor);
		applyNodeStyle(graphNode, descriptor.Kind);
		graphNode.ResetSize();

		activeGraphNodes[descriptor.NodeId] = graphNode;
		return graphNode;
	}

	// 创建仅在选中时显示的删除按钮。
	private Button createDeleteButton(string nodeId)
	{
		Button deleteButton = new()
		{
			Text = "删除",
			Visible = false,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		deleteButton.Pressed += () => deleteNodeRequested(nodeId);
		deleteButtonByNodeId[nodeId] = deleteButton;
		return deleteButton;
	}

	// 根据节点类型设置外观。
	private static void applyNodeStyle(GraphNode graphNode, string nodeKind)
	{
		StyleBoxFlat panelStyle = new()
		{
			BgColor = new Color(0.12f, 0.16f, 0.2f),
			BorderColor = new Color(0.39f, 0.69f, 0.92f),
			BorderWidthBottom = 2,
			BorderWidthTop = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
		graphNode.AddThemeStyleboxOverride("panel", panelStyle);
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

		if (canvas.IsNodeConnected(fromNodeName!, 0, toNodeName!, 0))
		{
			return;
		}

		canvas.ConnectNode(fromNodeName!, 0, toNodeName!, 0);
	}

	// 画布可视节点描述。
	private sealed record FlowToolVisualNodeDescriptor(string NodeId, string DisplayName, string Kind, string DetailText);
}