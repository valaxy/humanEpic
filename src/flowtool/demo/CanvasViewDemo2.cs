using Godot;
using System.Collections.Generic;

/// <summary>
/// CanvasView 的演示入口 2，使用直接构造数据而不是反射提取。
/// </summary>
[GlobalClass]
public partial class CanvasViewDemo2 : PanelContainer
{
	// 演示画布领域模型。
	private readonly TopologyCanvas topologyCanvas = TopologyCanvas.Instance;
	// 节点拖拽控制器。
	private NodeDraggable nodeDraggable = null!;
	// 演示视图节点。
	private CanvasView canvasView = null!;
	// 滚轮缩放控制器。
	private MouseWheelZoomable mouseWheelZoomable = null!;

	/// <summary>
	/// 初始化演示界面并装载演示数据。
	/// </summary>
	public override void _Ready()
	{
		canvasView = GetNode<CanvasView>("CanvasView/Canvas/MainViewport/CanvasRoot/WorldLayer");
		reloadDemoScope();
		canvasView.NodeSelect += onNodeSelectedRecognized;
		canvasView.SelectedNodeDelete += onNodeDeleteRequested;
		canvasView.Setup(topologyCanvas);
		mouseWheelZoomable = new MouseWheelZoomable(canvasView);
		nodeDraggable = new NodeDraggable(canvasView, topologyCanvas);
	}

	// 重新加载演示作用域。
	private void reloadDemoScope()
	{
		MetricScope selectedScope = createDirectDemoScope();
		topologyCanvas.Reload(selectedScope);
		canvasView.ResetCameraToContentCenter();
	}

	// 创建直接构造的演示作用域。
	private MetricScope createDirectDemoScope()
	{
		IReadOnlyList<Metric> metrics = new List<Metric>
		{
			new Metric("populationDelta", "人口净变化", nameof(CanvasViewDemo2)),
			new Metric("population", "总人口", nameof(CanvasViewDemo2)),
			new Metric("laborSupply", "劳动力供给", nameof(CanvasViewDemo2)),
			new Metric("output", "总产出", nameof(CanvasViewDemo2)),
			new Metric("consumptionBudget", "人均消费预算", nameof(CanvasViewDemo2)),
			new Metric("demandIndex", "需求热度", nameof(CanvasViewDemo2))
		};

		IReadOnlyList<(string input, string output)> rawMetricRelations = new List<(string input, string output)>
		{
			("populationDelta", "population"),
			("population", "laborSupply"),
			("laborSupply", "output"),
			("output", "consumptionBudget"),
			("population", "consumptionBudget"),
			("consumptionBudget", "demandIndex")
		};

		return new MetricScope(
			name: "canvas_view_demo2_scope",
			displayName: "Canvas View Demo 2",
			metrics: metrics,
			rawMetricRelations: rawMetricRelations);
	}

	// 验证节点选中信号。
	private void onNodeSelectedRecognized(string nodeId, Vector2 graphPointerPosition)
	{
		canvasView.UpdateSelectedNode(nodeId);
		if (string.IsNullOrWhiteSpace(nodeId))
		{
			canvasView.ClearDropShadow();
			return;
		}

		canvasView.SetDropShadow(nodeId, topologyCanvas.Nodes[nodeId].Position);
		GD.Print($"[CanvasViewDemo2] 选中节点: {nodeId}");
	}

	// 验证节点删除信号。
	private void onNodeDeleteRequested(string nodeId)
	{
		topologyCanvas.DeactiveNode(nodeId);
		canvasView.UpdateSelectedNode(string.Empty);
		canvasView.ClearDropShadow();
		canvasView.QueueRedraw();
		GD.Print($"[CanvasViewDemo2] 删除节点: {nodeId}");
	}
}
