using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// flowtool 左侧类列表组件。
/// </summary>
public sealed class FlowToolLayoutScopePanelController
{
	// 列表控件。
	private readonly ItemList layoutScopeList;
	// 列表更新锁。
	private bool isUpdatingSelection;

	/// <summary>
	/// 当前是否处于列表更新中。
	/// </summary>
	public bool IsUpdatingSelection => isUpdatingSelection;

	/// <summary>
	/// 构造类列表组件。
	/// </summary>
	public FlowToolLayoutScopePanelController(ItemList layoutScopeList)
	{
		this.layoutScopeList = layoutScopeList;
	}

	/// <summary>
	/// 绑定列表选择事件。
	/// </summary>
	public void BindSelection(Action<long> onSelected)
	{
		layoutScopeList.ItemSelected += selectedIndex => onSelected(selectedIndex);
	}

	/// <summary>
	/// 渲染类列表项并同步当前选中项。
	/// </summary>
	public void RenderScopes(IReadOnlyList<FlowToolLayoutScopeItem> scopes, string selectedScopeKey)
	{
		isUpdatingSelection = true;
		layoutScopeList.Clear();
		scopes
			.Select(static scope => scope.DisplayName)
			.ToList()
			.ForEach(displayName => layoutScopeList.AddItem(displayName));

		int selectedScopeIndex = scopes
			.Select((scope, index) => new { scope, index })
			.Where(item => item.scope.ScopeKey == selectedScopeKey)
			.Select(static item => item.index)
			.DefaultIfEmpty(0)
			.First();
		if (scopes.Count > 0)
		{
			layoutScopeList.Select(selectedScopeIndex);
		}

		isUpdatingSelection = false;
	}
}

/// <summary>
/// flowtool 类列表项。
/// </summary>
public sealed record FlowToolLayoutScopeItem(string ScopeKey, string DisplayName);