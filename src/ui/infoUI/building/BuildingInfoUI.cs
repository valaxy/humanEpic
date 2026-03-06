using Godot;

/// <summary>
/// 建筑信息 UI 控制器，负责监听选中地格并展示建筑详细数据。
/// </summary>
[GlobalClass]
public partial class BuildingInfoUI : Node
{
	// 共享信息面板。
	private InfoUI infoUi = null!;
	// 建筑集合。
	private BuildingCollection buildingCollection = null!;
	// 当前选中的建筑。
	private Building selectedBuilding = null!;
	// 当前是否有选中建筑。
	private bool hasSelectedBuilding;

	/// <summary>
	/// 初始化控制器。
	/// </summary>
	public void Setup(BuildingCollection buildingCollection, InfoUI infoUi)
	{
		this.infoUi = infoUi;
		this.buildingCollection = buildingCollection;
		hasSelectedBuilding = false;
	}

	/// <summary>
	/// 绑定选中管理器。
	/// </summary>
	public void BindSelection(GroundView selection)
	{
		selection.CellSelected += OnCellSelected;
		selection.SelectionCleared += OnSelectionCleared;
	}

	/// <summary>
	/// 地格选中回调。
	/// </summary>
	public void OnCellSelected(Vector2I cellPos)
	{
		if (!buildingCollection.HasKey(cellPos))
		{
			clearBuildingSelection();
			return;
		}

		selectedBuilding = buildingCollection.Get(cellPos);
		hasSelectedBuilding = true;
		refreshBuildingInfo();
	}

	/// <summary>
	/// 选中清理回调。
	/// </summary>
	public void OnSelectionCleared()
	{
		clearBuildingSelection();
	}

	// 清理建筑选中并隐藏信息面板。
	private void clearBuildingSelection()
	{
		if (!hasSelectedBuilding)
		{
			infoUi.HideInfo("建筑信息");
			return;
		}

		hasSelectedBuilding = false;
		selectedBuilding = null!;
		infoUi.HideInfo("建筑信息");
	}

	// 刷新建筑信息到通用信息面板。
	private void refreshBuildingInfo()
	{
		if (!hasSelectedBuilding)
		{
			return;
		}

		InfoData data = selectedBuilding.GetInfoData();
		infoUi.ShowInfo("建筑信息", data);
	}
}
