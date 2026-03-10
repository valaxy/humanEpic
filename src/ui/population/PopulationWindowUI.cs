using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 人口窗口 UI，负责入口按钮、人口列表和人口详情展示。
/// </summary>
[GlobalClass]
public partial class PopulationWindowUI : CanvasLayer
{
	// 人口入口按钮。
	private Button entryButton = null!;
	// 可拖拽窗口。
	private DraggableWindow draggableWindow = null!;
	// 人口列表。
	private ItemList populationList = null!;
	// 人口详情内容块。
	private InfoContentBlock detailContentBlock = null!;
	// 人口集合。
	private PopulationCollection populations = null!;
	// 列表缓存。
	private List<Population> listCache = new List<Population>();

	/// <summary>
	/// 初始化节点与交互。
	/// </summary>
	public override void _Ready()
	{
		entryButton = GetNode<Button>("%PopulationEntryButton");
		draggableWindow = GetNode<DraggableWindow>("%PopulationWindow");
		populationList = GetNode<ItemList>("%PopulationList");
		detailContentBlock = GetNode<InfoContentBlock>("%PopulationDetailContent");

		draggableWindow.SetTitle("人口列表");
		draggableWindow.Visible = false;
		entryButton.Pressed += onEntryButtonPressed;
		draggableWindow.CloseRequested += onCloseRequested;
		populationList.ItemSelected += onPopulationSelected;
	}

	/// <summary>
	/// 绑定游戏世界数据。
	/// </summary>
	public void Setup(GameWorld world)
	{
		populations = world.Populations;
		refreshPopulationList();
	}

	// 入口按钮点击。
	private void onEntryButtonPressed()
	{
		refreshPopulationList();
		draggableWindow.Visible = true;
	}

	// 窗口关闭。
	private void onCloseRequested()
	{
		draggableWindow.Visible = false;
	}

	// 人口列表选中。
	private void onPopulationSelected(long index)
	{
		if (index < 0 || index >= listCache.Count)
		{
			detailContentBlock.Clear();
			return;
		}

		Population population = listCache[(int)index];
		detailContentBlock.Render(population.GetInfoData());
	}

	// 刷新人口列表。
	private void refreshPopulationList()
	{
		populationList.Clear();
		listCache = populations.GetAll()
			.OrderBy(population => population.Id)
			.ToList();

		listCache
			.Select(population => $"{population.Name} ({population.Count})")
			.ToList()
			.ForEach(text => populationList.AddItem(text));

		if (listCache.Count == 0)
		{
			detailContentBlock.Clear();
			return;
		}

		populationList.Select(0);
		onPopulationSelected(0);
	}
}
