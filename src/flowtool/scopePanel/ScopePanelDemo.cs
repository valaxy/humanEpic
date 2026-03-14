using Godot;
using System;
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
	// 演示作用域列表。
	private IReadOnlyList<TopologyScope> demoScopes = Array.Empty<TopologyScope>();

	/// <summary>
	/// 初始化演示并渲染示例作用域。
	/// </summary>
	public override void _Ready()
	{
		scopePanel = GetNode<ScopePanel>("ScopePanel");
		demoScopes = createDemoScopes();
		scopePanel.ScopeSelected += onScopeSelected;
		scopePanel.Update(demoScopes, "scope:population");
	}

	// 构造演示作用域数据。
	private static IReadOnlyList<TopologyScope> createDemoScopes()
	{
		IReadOnlyList<TopologyScope> scopes = new[]
		{
			new TopologyScope("all", "全部"),
			new TopologyScope("scope:population", "Population", new[]
			{
				new MetricNode("population.total", "PopulationTotal", "总人口", "System.Single", "Demo.Population")
			}, Array.Empty<MetricEdge>()),
			new TopologyScope("scope:market", "Market", new[]
			{
				new MetricNode("market.price", "MarketPrice", "价格水平", "System.Single", "Demo.Market")
			}, Array.Empty<MetricEdge>())
		};
		return scopes.ToList();
	}

	// 打印当前选中的作用域信息。
	private void onScopeSelected(long selectedIndex)
	{
		TopologyScope selectedScope = demoScopes[(int)selectedIndex];
		GD.Print($"[ScopePanelDemo] ScopeSelected => {selectedScope.ScopeKey}/{selectedScope.DisplayName}");
	}
}
