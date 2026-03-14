using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 左侧类列表组件。
/// </summary>
[GlobalClass]
public partial class ScopePanel : VBoxContainer
{
	/// <summary>
	/// 当列表选项被选中时发出。
	/// </summary>
	[Signal]
	public delegate void ScopeSelectedEventHandler(long selectedIndex);

	// 列表控件。
	private ItemList layoutScopeList = null!;

	/// <summary>
	/// 组件初始化。
	/// </summary>
	public override void _Ready()
	{
		layoutScopeList = GetNode<ItemList>("LayoutScopeList");
		layoutScopeList.ItemSelected += onItemSelected;
	}

	/// <summary>
	/// 渲染类列表项并同步当前选中项。
	/// </summary>
	public void Update(IReadOnlyList<Topology> scopes, string selectedScopeKey)
	{
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
	}

	// 转发 ItemList 交互为组件级信号。
	private void onItemSelected(long selectedIndex)
	{
		EmitSignal(SignalName.ScopeSelected, selectedIndex);
	}
}

