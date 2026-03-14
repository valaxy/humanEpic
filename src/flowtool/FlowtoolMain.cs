using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Flowtool 主模块，负责串联 ScopePanel、CanvasView 与 UnassignedPoolView。
/// </summary>
[GlobalClass]
public partial class FlowtoolMain : Control
{
	// 拓扑画布数据。
	private readonly TopologyCanvas topologyCanvas = TopologyCanvas.Instance;
	// 作用域列表面板。
	private ScopePanelView scopePanel = null!;
	// 画布视图。
	private CanvasView canvasView = null!;
	// 未分配池视图。
	private UnassignedPoolView unassignedPoolView = null!;
	// 画布投放覆盖层。
	private CanvasDropOverlay canvasDropOverlay = null!;
	// 系统作用域数据。
	private GameSystem gameSystem = null!;

	/// <summary>
	/// 初始化主模块并建立三栏联动。
	/// </summary>
	public override void _Ready()
	{
		scopePanel = GetNode<ScopePanelView>("Layout/ScopePanel");
		canvasView = GetNode<CanvasView>("Layout/CanvasCenter/CanvasView/Canvas/MainViewport/CanvasRoot/WorldLayer");
		unassignedPoolView = GetNode<UnassignedPoolView>("Layout/UnassignedPoolView");
		canvasDropOverlay = GetNode<CanvasDropOverlay>("Layout/CanvasCenter/CanvasDropOverlay");
		gameSystem = MetricInfoExtractor.ExtractFromCurrentAssembly();

		canvasView.Setup(topologyCanvas);

		scopePanel.ScopeSelected += onScopeSelected;
		canvasView.NodeSelect += onCanvasNodeSelected;
		canvasView.SelectedNodeDelete += onCanvasNodeDeleteRequested;
		canvasDropOverlay.NodeRestoreRequested += onCanvasNodeRestoreRequested;

		initializeFirstScope();
	}

	// 初始化并显示第一个作用域。
	private void initializeFirstScope()
	{
		IReadOnlyList<MetricScope> scopes = gameSystem.Scopes.Values.ToList();
		if (scopes.Count == 0)
		{
			return;
		}

		string firstScopeName = scopes[0].Name;
		scopePanel.Setup(gameSystem, firstScopeName);
		applyScope(firstScopeName);
	}

	// 作用域切换事件处理。
	private void onScopeSelected(string selectedScopeName)
	{
		applyScope(selectedScopeName);
	}

	// 应用指定作用域并刷新中间与右侧视图。
	private void applyScope(string selectedScopeName)
	{
		if (gameSystem.Scopes.ContainsKey(selectedScopeName) == false)
		{
			return;
		}

		MetricScope selectedScope = gameSystem.Scopes[selectedScopeName];
		topologyCanvas.Reload(selectedScope);
		canvasView.UpdateSelectedNode(string.Empty);
		canvasView.ClearDropShadow();
		canvasView.ResetCameraToContentCenter();
		canvasView.QueueRedraw();
		unassignedPoolView.Update(topologyCanvas);
	}

	// 画布节点选中事件处理。
	private void onCanvasNodeSelected(string nodeId, Vector2 graphPointerPosition)
	{
		canvasView.UpdateSelectedNode(nodeId);
		if (string.IsNullOrWhiteSpace(nodeId))
		{
			canvasView.ClearDropShadow();
			return;
		}

		canvasView.SetDropShadow(nodeId, topologyCanvas.Nodes[nodeId].Position);
	}

	// 画布节点删除事件处理。
	private void onCanvasNodeDeleteRequested(string nodeId)
	{
		topologyCanvas.DeactiveNode(nodeId);
		canvasView.UpdateSelectedNode(string.Empty);
		canvasView.ClearDropShadow();
		canvasView.QueueRedraw();
		unassignedPoolView.Update(topologyCanvas);
	}

	// 未分配池拖入画布后的节点恢复处理。
	private void onCanvasNodeRestoreRequested(string nodeId, Vector2 canvasLocalPointerPosition)
	{
		if (topologyCanvas.Nodes.ContainsKey(nodeId) == false)
		{
			return;
		}

		Vector2 pointerGraphPosition = canvasView.MapCanvasLocalPointerToGraph(canvasLocalPointerPosition);
		Vector2 restoredNodePosition = pointerGraphPosition - (topologyCanvas.NodeSize * 0.5f);
		topologyCanvas.Nodes[nodeId].IsActive = true;
		topologyCanvas.UpdateNodePosition(nodeId, restoredNodePosition);
		canvasView.UpdateSelectedNode(nodeId);
		canvasView.SetDropShadow(nodeId, restoredNodePosition);
		canvasView.QueueRedraw();
		unassignedPoolView.Update(topologyCanvas);
	}
}
