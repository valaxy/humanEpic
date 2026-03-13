using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 左侧类列表组件。
/// </summary>
[GlobalClass]
public partial class ScopePanel : VBoxContainer
{
	// 列表控件。
	private ItemList layoutScopeList = null!;
	// 列表更新锁。
	private bool isUpdatingSelection;

	/// <summary>
	/// 当前是否处于列表更新中。
	/// </summary>
	public bool IsUpdatingSelection => isUpdatingSelection;

	/// <summary>
	/// 组件初始化。
	/// </summary>
	public override void _Ready()
	{
		layoutScopeList = GetNode<ItemList>("LayoutScopeList");
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
	public void Setup(IReadOnlyList<FlowToolLayoutScopeItem> scopes, string selectedScopeKey)
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

