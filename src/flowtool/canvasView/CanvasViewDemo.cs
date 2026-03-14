using Godot;
using System.Collections.Generic;

/// <summary>
/// CanvasView 的演示入口，用于验证基础绘制结果。
/// </summary>
[Tool]
[GlobalClass]
public partial class CanvasViewDemo : Node2D
{
	// 演示画布领域模型。
	private readonly TopologyCanvas demoTopology = new(1400f, 900f, 260f, 96f);
	// 演示视图节点。
	private CanvasView canvasView = null!;
	// 演示摄像机。
	private Camera2D demoCamera = null!;

	public override void _Ready()
	{
		canvasView = GetNode<CanvasView>("CanvasView");
		demoCamera = GetNode<Camera2D>("DemoCamera");
		demoCamera.MakeCurrent();
		canvasView.Initialize(demoTopology);
		seedDemoGraph();
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

	// 演示场景不包含交互逻辑，仅用于验证渲染输出。
}
