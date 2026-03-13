using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 右侧未分配池面板，负责展示当前作用域下未放置到画布的节点列表。
/// </summary>
[Tool]
[GlobalClass]
public partial class UnassignedPoolPanel : VBoxContainer
{
	// 未分配池列表容器。
	private VBoxContainer unassignedPoolList = null!;

	/// <summary>
	/// 组件初始化。
	/// </summary>
	public override void _Ready()
	{
		unassignedPoolList = GetNode<VBoxContainer>("PoolScrollContainer/UnassignedPoolList");
	}

	/// <summary>
	/// 渲染未分配池列表。
	/// </summary>
	public void Update(FlowToolTopology topology, IReadOnlyCollection<string> activeNodeIds)
	{
		unassignedPoolList.GetChildren().ToList().ForEach(static child => child.QueueFree());

		IReadOnlyList<UnassignedItem> metricItems = topology.Metrics
			.Where(metric => activeNodeIds.Contains(metric.NodeId) == false)
			.OrderBy(static metric => metric.DisplayName, System.StringComparer.Ordinal)
			.Select(createMetricPoolItem)
			.ToList();

		metricItems.ToList().ForEach(item => unassignedPoolList.AddChild(item));
	}

	// 创建指标池项。
	private static UnassignedItem createMetricPoolItem(FlowToolMetricNode metricNode)
	{
		UnassignedItem button = new();
		button.Setup($"{metricNode.DisplayName}", metricNode.NodeId);
		return button;
	}
}