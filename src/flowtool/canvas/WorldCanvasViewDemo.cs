using Godot;
using System.Collections.Generic;

/// <summary>
/// WorldCanvasView 的演示入口，用于验证基础绘制结果。
/// </summary>
[Tool]
[GlobalClass]
public partial class WorldCanvasViewDemo : Node2D
{
	// 演示画布领域模型。
	private readonly WorldCanvas worldCanvas = new(1400f, 900f, 260f, 96f);
	// 演示视图节点。
	private WorldCanvasView worldCanvasView = null!;
	// 演示摄像机。
	private Camera2D demoCamera = null!;

	public override void _Ready()
	{
		worldCanvasView = GetNode<WorldCanvasView>("WorldCanvasView");
		demoCamera = GetNode<Camera2D>("DemoCamera");
		demoCamera.MakeCurrent();
		bindCanvasSignals();
		worldCanvasView.Initialize(worldCanvas);
		seedDemoGraph();
		testCanvasSignals();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		worldCanvasView.HandleInputEvent(@event, demoCamera);
		if (@event is InputEventMouseButton mouseButton
			&& (mouseButton.ButtonIndex == MouseButton.WheelUp || mouseButton.ButtonIndex == MouseButton.WheelDown)
			&& mouseButton.Pressed)
		{
			GD.Print($"[Demo] Zoom => {demoCamera.Zoom}");
		}
	}

	// 绑定画布基础交互信号。
	private void bindCanvasSignals()
	{
		worldCanvasView.NodeSelected += nodeId => GD.Print($"[Demo] NodeSelected => {nodeId}");
		worldCanvasView.NodeDragged += (nodeId, position) => GD.Print($"[Demo] NodeDragged => {nodeId}@{position}");
		worldCanvasView.DeleteRequested += nodeId => GD.Print($"[Demo] DeleteRequested => {nodeId}");
		worldCanvasView.NodePayloadDropped += (nodeId, graphPosition) => GD.Print($"[Demo] NodePayloadDropped => {nodeId}@{graphPosition}");
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
		worldCanvas.UpdateGraph(nodesByNodeId, layoutByNodeId, edges);
		worldCanvasView.SetDropShadow("Output", new Vector2(940f, 420f));
		worldCanvasView.QueueRedraw();
	}

	// 触发并验证基础信号链路。
	private void testCanvasSignals()
	{
		InputEventMouseButton selectEvent = new()
		{
			Pressed = true,
			ButtonIndex = MouseButton.Left,
			Position = new Vector2(140f, 180f)
		};
		worldCanvasView.HandleInputEvent(selectEvent, demoCamera);

		InputEventMouseMotion dragEvent = new()
		{
			Position = new Vector2(200f, 240f),
			ButtonMask = MouseButtonMask.Left,
			Relative = new Vector2(60f, 60f)
		};
		worldCanvasView.HandleInputEvent(dragEvent, demoCamera);

		InputEventKey deleteEvent = new()
		{
			Pressed = true,
			Keycode = Key.Delete
		};
		worldCanvasView.HandleInputEvent(deleteEvent, demoCamera);

		worldCanvasView.NotifyNodePayloadDropped("Process", new Vector2(520f, 180f));
	}
}
