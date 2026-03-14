using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// UnassignedPoolPanel 的演示入口，用于验证未分配池渲染结果。
/// </summary>
[Tool]
[GlobalClass]
public partial class UnassignedPoolPanelDemo : Control
{
	// 演示面板。
	private UnassignedPoolPanel unassignedPoolPanel = null!;
	// 演示拓扑。
	private readonly GameSystem demoTopology = new(
		new List<MetricNode>
		{
			new("metric:population", "Population", "总人口", "System.Single", "Demo.Population"),
			new("metric:price", "Price", "价格水平", "System.Single", "Demo.Market"),
			new("metric:wage", "Wage", "工资水平", "System.Single", "Demo.Market")
		},
		new List<MetricEdge>
		{
			new("metric:population", "metric:price"),
			new("metric:price", "metric:wage")
		});

	/// <summary>
	/// 初始化并渲染演示数据。
	/// </summary>
	public override void _Ready()
	{
		unassignedPoolPanel = GetNode<UnassignedPoolPanel>("UnassignedPoolPanel");
		IReadOnlyCollection<string> activeNodeIds = new HashSet<string>(StringComparer.Ordinal)
		{
			"metric:population"
		};
		unassignedPoolPanel.Update(demoTopology.GetTopology(GameSystem.AllTopologyScopeKey), activeNodeIds);
		GD.Print("[UnassignedPoolPanelDemo] Rendered with 2 unassigned nodes.");
	}
}
