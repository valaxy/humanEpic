using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ScopePanel 的演示入口，用于验证作用域渲染和选中信号。
/// </summary>
[Tool]
[GlobalClass]
public partial class ScopePanelDemo : Control
{
	// 演示面板。
	private ScopePanel scopePanel = null!;
	// 演示系统数据。
	private GameSystem demoSystem = null!;

	/// <summary>
	/// 初始化演示并渲染示例作用域。
	/// </summary>
	public override void _Ready()
	{
		scopePanel = GetNode<ScopePanel>("ScopePanel");
		demoSystem = createDemoSystem();
		scopePanel.ScopeSelected += onScopeSelected;
		scopePanel.Setup(demoSystem, "scope:population");
	}

	// 构造演示系统数据。
	private static GameSystem createDemoSystem()
	{
		IReadOnlyList<MetricScope> scopes = new[]
		{
			new MetricScope(
				name: "scope:all",
				displayName: "湖南省",
				metrics: new List<Metric>(),
				rawMetricRelations: new List<(string input, string output)>()),
			new MetricScope(
				name: "scope:population",
				displayName: "Population",
				metrics: new List<Metric>
				{
					new Metric("population.total", "总人口", "Demo.Population"),
					new Metric("population.delta", "人口变化", "Demo.Population")
				},
				rawMetricRelations: new List<(string input, string output)>
				{
					("population.delta", "population.total")
				}),
			new MetricScope(
				name: "scope:market",
				displayName: "Market",
				metrics: new List<Metric>
				{
					new Metric("market.price", "价格水平", "Demo.Market")
				},
				rawMetricRelations: new List<(string input, string output)>())
		};

		return new GameSystem(scopes);
	}

	// 打印当前选中的作用域信息。
	private void onScopeSelected(string selectedScopeName)
	{
		MetricScope selectedScope = demoSystem.Scopes[selectedScopeName];
		GD.Print($"[ScopePanelDemo] ScopeSelected => {selectedScope.Name}/{selectedScope.DisplayName}");
	}
}
