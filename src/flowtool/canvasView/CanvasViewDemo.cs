using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CanvasView 的演示入口，用于验证基础绘制结果。
/// </summary>
[GlobalClass]
public partial class CanvasViewDemo : PanelContainer
{
	// 演示画布领域模型。
	private readonly TopologyCanvas demoTopology = new(1400f, 900f, 260f, 96f);
	// 演示视图节点。
	private CanvasView canvasView = null!;

	public override void _Ready()
	{
		canvasView = GetNode<CanvasView>("CanvasView/Canvas/MainViewport/CanvasRoot/WorldLayer");
		canvasView.NodePayloadDropped += onNodePayloadDropped;
		canvasView.NodeSelectedRecognized += onNodeSelectedRecognized;
		canvasView.SelectedNodeDeleteRequested += onNodeDeleteRequested;
		canvasView.Initialize(demoTopology);
		seedDemoGraph();
		canvasView.UpdateSelectedNode("Process");
		canvasView.SyncViewportSize();
		Vector2I viewportSize = canvasView.GetCanvasViewportSize();
		GD.Print($"[CanvasViewDemo] viewport size = {viewportSize.X}x{viewportSize.Y}");
		canvasView.EmitSignal(CanvasView.SignalName.NodePayloadDropped, "Output", new Vector2(980f, 420f));
	}

	// 初始化演示节点与连线。
	private void seedDemoGraph()
	{
		IReadOnlyDictionary<string, MetricNode> nodesByNodeId = new Dictionary<string, MetricNode>
		{
			["Input"] = new MetricNode("Input", "Input", "输入节点", "System.Single", "Demo.Flow"),
			["Process"] = new MetricNode("Process", "Process", "处理节点", "System.Single", "Demo.Flow"),
			["Output"] = new MetricNode("Output", "Output", "输出节点", "System.Single", "Demo.Flow")
		};
		IReadOnlyDictionary<string, Vector2> layoutByNodeId = new Dictionary<string, Vector2>
		{
			["Input"] = new Vector2(120f, 160f),
			["Process"] = new Vector2(520f, 180f),
			["Output"] = new Vector2(940f, 420f)
		};
		IReadOnlyList<MetricEdge> edges = new List<MetricEdge>
		{
			new("Input", "Process"),
			new("Process", "Output")
		};
		demoTopology.UpdateGraph(nodesByNodeId, layoutByNodeId, edges);
		canvasView.SetDropShadow("Output", new Vector2(940f, 420f));
		canvasView.QueueRedraw();
	}

	// 处理演示中的节点落点事件。
	private void onNodePayloadDropped(string nodeId, Vector2 graphPosition)
	{
		IReadOnlyDictionary<string, Vector2> nextLayout = demoTopology.NodeLayout
			.ToDictionary(
				pair => pair.Key,
				pair => pair.Key == nodeId ? graphPosition : pair.Value,
				StringComparer.Ordinal);
		demoTopology.UpdateGraph(demoTopology.Nodes, nextLayout, demoTopology.Edges);
		canvasView.SetDropShadow(nodeId, graphPosition);
		canvasView.QueueRedraw();
	}

	// 验证节点选中信号。
	private void onNodeSelectedRecognized(string nodeId, Vector2 graphPointerPosition)
	{
		if (string.IsNullOrWhiteSpace(nodeId))
		{
			return;
		}

		OS.Alert($"选中节点: {nodeId}", "CanvasViewDemo");
	}

	// 验证节点删除信号。
	private void onNodeDeleteRequested(string nodeId)
	{
		OS.Alert($"删除节点: {nodeId}", "CanvasViewDemo");
	}
}
