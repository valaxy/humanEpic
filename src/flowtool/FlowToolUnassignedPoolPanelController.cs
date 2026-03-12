using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// flowtool 右侧未分配池组件。
/// </summary>
public sealed class FlowToolUnassignedPoolPanelController
{
	// 过程节点类型标识。
	private const string processNodeKind = "process";
	// 指标节点类型标识。
	private const string metricNodeKind = "metric";

	// 未分配池列表容器。
	private readonly VBoxContainer unassignedPoolList;
	// 状态栏文本。
	private readonly Label statusLabel;

	/// <summary>
	/// 构造未分配池组件。
	/// </summary>
	public FlowToolUnassignedPoolPanelController(VBoxContainer unassignedPoolList, Label statusLabel)
	{
		this.unassignedPoolList = unassignedPoolList;
		this.statusLabel = statusLabel;
	}

	/// <summary>
	/// 渲染未分配池列表。
	/// </summary>
	public void RenderPool(FlowToolTopology topology, IReadOnlyCollection<string> activeNodeIds)
	{
		unassignedPoolList.GetChildren().ToList().ForEach(static child => child.QueueFree());

		IReadOnlyList<FlowToolPoolItemButton> processItems = topology.Processes
			.Where(process => activeNodeIds.Contains(process.NodeId) == false)
			.OrderBy(static process => process.DisplayName, System.StringComparer.Ordinal)
			.Select(createProcessPoolItem)
			.ToList();

		IReadOnlyList<FlowToolPoolItemButton> metricItems = topology.Metrics
			.Where(metric => activeNodeIds.Contains(metric.NodeId) == false)
			.OrderBy(static metric => metric.DisplayName, System.StringComparer.Ordinal)
			.Select(createMetricPoolItem)
			.ToList();

		processItems.Concat(metricItems).ToList().ForEach(item => unassignedPoolList.AddChild(item));
	}

	/// <summary>
	/// 更新状态栏文本。
	/// </summary>
	public void SetStatus(string text)
	{
		statusLabel.Text = text;
	}

	// 创建过程池项。
	private static FlowToolPoolItemButton createProcessPoolItem(FlowToolProcessNode processNode)
	{
		FlowToolPoolItemButton button = new();
		button.Setup($"[过程] {processNode.DisplayName}", processNode.NodeId, processNodeKind);
		return button;
	}

	// 创建指标池项。
	private static FlowToolPoolItemButton createMetricPoolItem(FlowToolMetricNode metricNode)
	{
		FlowToolPoolItemButton button = new();
		button.Setup($"[指标] {metricNode.DisplayName}", metricNode.NodeId, metricNodeKind);
		return button;
	}
}