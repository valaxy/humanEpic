using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 人口窗口 UI，负责入口按钮、人口列表和人口详情展示。
/// </summary>
[GlobalClass]
public partial class PopulationWindowUI : CanvasLayer
{
	/// <summary>
	/// 打开窗口后全量刷新间隔（秒）。
	/// </summary>
	[Export(PropertyHint.Range, "0.1,10,0.1")]
	public double RefreshIntervalSeconds { get; set; } = 1.0;

	/// <summary>
	/// 人口窗口显隐变化时发出。
	/// </summary>
	[Signal]
	public delegate void WindowVisibilityChangedEventHandler(bool visible);

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
	// 定时刷新计时器。
	private Timer refreshTimer = null!;

	/// <summary>
	/// 初始化节点与交互。
	/// </summary>
	public override void _Ready()
	{
		draggableWindow = GetNode<DraggableWindow>("%PopulationWindow");
		populationList = GetNode<ItemList>("%PopulationList");
		detailContentBlock = GetNode<InfoContentBlock>("%PopulationDetailContent");

		draggableWindow.SetTitle("人口列表");
		draggableWindow.Visible = false;
		draggableWindow.CloseRequested += onCloseRequested;
		populationList.ItemSelected += onPopulationSelected;
	}

 
	/// <summary>
	/// 直接绑定人口集合
	/// </summary>
	public void Setup(PopulationCollection populationCollection)
	{
		populations = populationCollection;
		refreshPopulationList();
	}

	/// <summary>
	/// 当前窗口是否可见。
	/// </summary>
	public bool IsWindowVisible => draggableWindow.Visible;

	/// <summary>
	/// 设置窗口显示状态。
	/// </summary>
	public void SetWindowVisible(bool visible)
	{
		if (visible)
		{
			GD.Print($"[UI] PopulationWindow opened. refresh interval={RefreshIntervalSeconds:0.0}s");
			ensureRefreshTimer();
			refreshTimer.Start();
			refreshPopulationList();
		}
		else
		{
			GD.Print("[UI] PopulationWindow closed. refresh timer removed.");
			releaseRefreshTimer();
		}

		draggableWindow.Visible = visible;
		EmitSignal(SignalName.WindowVisibilityChanged, visible);
	}

	/// <summary>
	/// 打开人口窗口。
	/// </summary>
	public void OpenWindow()
	{
		SetWindowVisible(true);
	}

	// 窗口关闭。
	private void onCloseRequested()
	{
		SetWindowVisible(false);
	}

	// 人口列表选中。
	private void onPopulationSelected(long index)
	{
		if (populations == null)
		{
			detailContentBlock.Clear();
			return;
		}

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
		if (populations == null)
		{
			GD.PushWarning("[UI] PopulationWindow refresh skipped because Setup() has not been called.");
			populationList.Clear();
			detailContentBlock.Clear();
			return;
		}

		populationList.Clear();
		long selectedIndex = populationList.GetSelectedItems().FirstOrDefault(-1);
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

		long validIndex = selectedIndex >= 0 && selectedIndex < listCache.Count ? selectedIndex : 0;
		populationList.Select((int)validIndex);
		onPopulationSelected(validIndex);
	}

	// 定时全量刷新。
	private void onRefreshTimerTimeout()
	{
		refreshPopulationList();
	}

	// 创建并启动定时器。
	private void ensureRefreshTimer()
	{
		if (refreshTimer != null)
		{
			refreshTimer.WaitTime = RefreshIntervalSeconds;
			return;
		}

		refreshTimer = new Timer
		{
			Autostart = false,
			OneShot = false,
			WaitTime = RefreshIntervalSeconds
		};
		refreshTimer.Timeout += onRefreshTimerTimeout;
		AddChild(refreshTimer);
	}

	// 停止并移除定时器。
	private void releaseRefreshTimer()
	{
		if (refreshTimer == null)
		{
			return;
		}

		refreshTimer.Stop();
		refreshTimer.Timeout -= onRefreshTimerTimeout;
		refreshTimer.QueueFree();
		refreshTimer = null!;
	}
}
