using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CanvasView 的演示入口，用于验证基础绘制结果。
/// </summary>
[GlobalClass]
public partial class CanvasViewDemo1 : PanelContainer
{
	// 演示画布领域模型。
	private readonly TopologyCanvas topologyCanvas = TopologyCanvas.Instance;
	// 演示视图节点。
	private CanvasView canvasView = null!;

	public override void _Ready()
	{
		canvasView = GetNode<CanvasView>("CanvasView/Canvas/MainViewport/CanvasRoot/WorldLayer");
		reloadDemoScope();
		canvasView.NodeSelect += onNodeSelectedRecognized;
		canvasView.SelectedNodeDelete += onNodeDeleteRequested;
		canvasView.Setup(topologyCanvas);
	}

	// 重新加载演示作用域。
	private void reloadDemoScope()
	{
		GameSystem gameSystem = MetricInfoExtractor.ExtractFromCurrentAssembly();
		IReadOnlyList<MetricScope> availableScopes = gameSystem.Scopes.Values.ToList();
		if (availableScopes.Count == 0)
		{
			return;
		}

		MetricScope selectedScope = availableScopes
			.Where(scope => scope.Name == nameof(Sample1))
			.DefaultIfEmpty(availableScopes[0])
			.First();
		topologyCanvas.Reload(selectedScope);
		canvasView.ResetCameraToContentCenter();
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
		OS.Alert($"选中节点: {nodeId}", "CanvasViewDemo");
	}

	// 验证节点删除信号。
	private void onNodeDeleteRequested(string nodeId)
	{
		topologyCanvas.DeactiveNode(nodeId);
		canvasView.UpdateSelectedNode(string.Empty);
		canvasView.ClearDropShadow();
		canvasView.QueueRedraw();
		OS.Alert($"删除节点: {nodeId}", "CanvasViewDemo");
	}
}
