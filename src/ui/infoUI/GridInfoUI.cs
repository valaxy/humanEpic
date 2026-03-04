using Godot;

/// <summary>
/// 地格信息 UI 控制器，负责展示悬浮/选中地格的地表与覆盖物信息。
/// </summary>
[GlobalClass]
public partial class GridInfoUI : Node
{
	// 左侧信息面板。
	private InfoUI infoUiLeft = null!;
	// 右侧信息面板。
	private InfoUI infoUiRight = null!;
	// 地形数据引用。
	private Ground ground = null!;
	// 当前激活地格。
	private Vector2I activeCellPos = Vector2I.Zero;
	// 当前是否存在激活地格。
	private bool hasActiveCell;

	/// <summary>
	/// 初始化控制器。
	/// </summary>
	public void Setup(InfoUI infoUiLeft, InfoUI infoUiRight, Ground ground)
	{
		this.infoUiLeft = infoUiLeft;
		this.infoUiRight = infoUiRight;
		this.ground = ground;
		hasActiveCell = false;
	}

	/// <summary>
	/// 当地格被悬浮时刷新显示。
	/// </summary>
	public void OnCellHovered(Vector2I cellPos)
	{
		activeCellPos = cellPos;
		hasActiveCell = true;
		updateInfoDisplay();
	}

	/// <summary>
	/// 当地格被选中时刷新显示。
	/// </summary>
	public void OnCellSelected(Vector2I cellPos)
	{
		activeCellPos = cellPos;
		hasActiveCell = true;
		updateInfoDisplay();
	}

	/// <summary>
	/// 当悬浮清理时隐藏信息。
	/// </summary>
	public void OnCellHoverCleared()
	{
		hasActiveCell = false;
		hideGridInfo();
	}

	/// <summary>
	/// 当选中清理时隐藏信息。
	/// </summary>
	public void OnSelectionCleared()
	{
		hasActiveCell = false;
		hideGridInfo();
	}

	// 刷新地格信息显示。
	private void updateInfoDisplay()
	{
		if (!hasActiveCell)
		{
			return;
		}

		if (!ground.IsInsideGround(activeCellPos))
		{
			return;
		}

		Grid grid = ground.GetGrid(activeCellPos.X, activeCellPos.Y);
		showGridInfo(activeCellPos, grid);
	}

	// 组装并展示地格信息。
	private void showGridInfo(Vector2I cellPos, Grid grid)
	{
		SurfaceTemplate surfaceTemplate = SurfaceTemplate.GetTemplate(grid.SurfaceType);
		OverlayTemplate overlayTemplate = OverlayTemplate.GetTemplate(grid.OverlayType);

		InfoData basicInfo = new InfoData();
		basicInfo.AddText("坐标", $"({cellPos.X}, {cellPos.Y})");
		basicInfo.AddText("地表", surfaceTemplate.Name);

		InfoData leftPayload = new InfoData();
		leftPayload.AddGroup("基础地层", basicInfo);

		if (grid.OverlayType == OverlayType.Enums.NONE)
		{
			infoUiRight.HideInfo("覆盖物信息");
		}
		else
		{
			Overlay overlay = grid.Overlay;
			InfoData overlayInfo = new InfoData();
			overlayInfo.AddText("类型", overlayTemplate.Name);
			overlayInfo.AddProgress("资源量", overlay.AmountRatio, $"{overlay.Amount:0.0} / {overlay.MaxAmount:0.0}");

			InfoData rightPayload = new InfoData();
			rightPayload.AddGroup("覆盖物详情", overlayInfo);
			infoUiRight.ShowInfo("覆盖物信息", rightPayload);
		}

		infoUiLeft.ShowInfo("地格信息", leftPayload);
	}

	// 隐藏地格相关信息。
	private void hideGridInfo()
	{
		infoUiLeft.HideInfo("地格信息");
		infoUiRight.HideInfo("覆盖物信息");
	}
}
