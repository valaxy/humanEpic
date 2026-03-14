using Godot;
using System.Collections.Generic;

/// <summary>
/// UnassignedPoolPanel 的演示入口，用于验证未分配池渲染结果。
/// </summary>
[GlobalClass]
public partial class UnassignedPoolViewDemo : Control
{
	// 演示面板。
	private UnassignedPoolView unassignedPoolView = null!;
	// 演示画布数据。
	private readonly TopologyCanvas topologyCanvas = TopologyCanvas.Instance;

	/// <summary>
	/// 初始化并渲染演示数据。
	/// </summary>
	public override void _Ready()
	{
		unassignedPoolView = GetNode<UnassignedPoolView>("UnassignedPoolView");
		MetricScope demoScope = createDemoScope();
		topologyCanvas.Reload(demoScope);
		topologyCanvas.Nodes["metric:population"].IsActive = true;
		topologyCanvas.Nodes["metric:price"].IsActive = false;
		topologyCanvas.Nodes["metric:wage"].IsActive = false;
		unassignedPoolView.Update(topologyCanvas);
		GD.Print("[UnassignedPoolViewDemo] Rendered with 2 unassigned nodes. Drag them to drop target for payload test.");
	}

	// 构造演示作用域数据。
	private static MetricScope createDemoScope()
	{
		IReadOnlyList<Metric> metrics = new List<Metric>
		{
			new Metric("metric:population", "总人口", "Demo.Population"),
			new Metric("metric:price", "价格水平", "Demo.Market"),
			new Metric("metric:wage", "工资水平", "Demo.Market")
		};

		IReadOnlyList<(string input, string output)> rawMetricRelations = new List<(string input, string output)>
		{
			("metric:population", "metric:price"),
			("metric:price", "metric:wage")
		};

		return new MetricScope(
			name: "unassigned_pool_demo",
			displayName: "UnassignedPool Demo",
			metrics: metrics,
			rawMetricRelations: rawMetricRelations);
	}
}
