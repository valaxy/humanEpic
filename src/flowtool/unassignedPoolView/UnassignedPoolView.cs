using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 右侧未分配池视图，负责展示当前作用域未激活节点。
/// </summary>
[GlobalClass]
public partial class UnassignedPoolView : VBoxContainer
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
	public void Update(TopologyCanvas canvas)
	{
		unassignedPoolList.GetChildren().ToList().ForEach(static child => child.QueueFree());

		IReadOnlyList<UnassignedItem> items = canvas.Nodes.Values
			.Where(node => !node.IsActive)
			.OrderBy(static node => node.Metric.Name, System.StringComparer.Ordinal)
			.Select(createMetricPoolItem)
			.ToList();

		items.ToList().ForEach(item => unassignedPoolList.AddChild(item));
	}

	// 创建指标池项。
	private static UnassignedItem createMetricPoolItem(TopologyNode metricNode)
	{
		UnassignedItem button = new();
		button.Setup(metricNode.Metric.Name, metricNode.Metric.Name);
		return button;
	}
}
