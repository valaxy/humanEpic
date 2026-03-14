using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 左侧拓扑作用域列表视图。
/// </summary>
[GlobalClass]
public partial class ScopePanelView : VBoxContainer
{
	/// <summary>
	/// 当列表选项被选中时发出。
	/// </summary>
	[Signal]
	public delegate void ScopeSelectedEventHandler(string selectedScopeName);

	// 列表控件。
	private ItemList layoutScopeList = null!;
	// 当前作用域快照。
	private IReadOnlyList<MetricScope> scopes = Array.Empty<MetricScope>();
	// 当前选中作用域名称。
	private string selectedScopeName = string.Empty;

	/// <summary>
	/// 组件初始化。
	/// </summary>
	public override void _Ready()
	{
		layoutScopeList = GetNode<ItemList>("LayoutScopeList");
		layoutScopeList.ItemSelected += onItemSelected;
	}

	/// <summary>
	/// 首次载入后初始化列表
	/// </summary>
	public void Setup(GameSystem gameSystem, string initialSelectedScopeName)
	{
		scopes = gameSystem.Scopes.Values.ToList();
		selectedScopeName = initialSelectedScopeName;

		layoutScopeList.Clear();
		scopes
			.Select(static scope => scope.DisplayName)
			.ToList()
			.ForEach(displayName => layoutScopeList.AddItem(displayName));

		int selectedScopeIndex = scopes
			.Select((scope, index) => new { scope, index })
			.Where(item => item.scope.Name == selectedScopeName)
			.Select(static item => item.index)
			.DefaultIfEmpty(0)
			.First();

		if (scopes.Count > 0)
		{
			layoutScopeList.Select(selectedScopeIndex);
			selectedScopeName = scopes[selectedScopeIndex].Name;
		}
	}

	// 转发 ItemList 交互为组件级信号。
	private void onItemSelected(long selectedIndex)
	{
		if (selectedIndex < 0 || selectedIndex >= scopes.Count)
		{
			return;
		}

		selectedScopeName = scopes[(int)selectedIndex].Name;
		EmitSignal(SignalName.ScopeSelected, selectedScopeName);
	}
}
