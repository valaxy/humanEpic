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
	private IReadOnlyList<Topology> demoScopes = Array.Empty<Topology>();

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
	private static IReadOnlyList<Topology> createDemoScopes()
	{
		IReadOnlyList<Topology> scopes = new[]
		{
			new Topology("all", "全部"),
			new Topology("scope:population", "Population", new[]
			{
				new MetricNode("population.total", "PopulationTotal", "总人口", "System.Single", "Demo.Population")
			}, Array.Empty<MetricEdge>()),
			new Topology("scope:market", "Market", new[]
			{
				new MetricNode("market.price", "MarketPrice", "价格水平", "System.Single", "Demo.Market")
			}, Array.Empty<MetricEdge>())
		};
		return scopes.ToList();
	}

	// 打印当前选中的作用域信息。
	private void onScopeSelected(long selectedIndex)
	{
		Topology selectedScope = demoScopes[(int)selectedIndex];
		GD.Print($"[ScopePanelDemo] ScopeSelected => {selectedScope.ScopeKey}/{selectedScope.DisplayName}");
	}
}
