using Godot;

/// <summary>
/// 编辑模式下的逻辑（地块、覆盖物、建筑放置）
/// </summary>
[GlobalClass]
public partial class GroundEditorNode : Node
{
	private GroundNode groundNode = null!;

	/// <summary>是否处于地表/覆盖物编辑模式</summary>
	public bool IsOverlayMode { get; set; } = false;

	/// <summary>是否处于建筑编辑模式</summary>
	public bool IsBuildingEditorActive { get; set; } = false;

	/// <summary>当前选中的建筑类型标识符</summary>
	public string CurrentSelectedBuilding { get; set; } = "";

	/// <summary>当前选中的国家ID（-1 表示自动选择）</summary>
	public int CurrentSelectedCountryId { get; set; } = -1;

	/// <summary>当前选中的地表类型</summary>
	public SurfaceType.Enums SelectedSurface { get; set; } = SurfaceType.Enums.GRASSLAND;

	/// <summary>当前选中的覆盖物类型</summary>
	public OverlayType.Enums SelectedOverlay { get; set; } = OverlayType.Enums.NONE;


	/// <summary>是否同步更新地图数据</summary>
	public bool IsUpdatingGround { get; set; } = true;

	/// <summary>
	/// 初始化
	/// </summary>
	public void Setup(GroundNode groundNode)
	{
		this.groundNode = groundNode;
	}

	/// <summary>
	/// 检查在当前模式下，选定并绘制该地块是否有效
	/// </summary>
	public bool IsCellSelectionValid(Vector2I cellPos)
	{
		if (IsOverlayMode && SelectedOverlay != OverlayType.Enums.NONE)
		{
			if (groundNode.BrushCursor == null)
			{
				return false;
			}

			Vector2I[] affected = groundNode.BrushCursor.GetAffectedCells(cellPos.X, cellPos.Y);
			foreach (Vector2I affectedCell in affected)
			{
				if (!groundNode.InteractionNode.IsWithinBounds(affectedCell))
				{
					continue;
				}

				if (isOverlayPlacementValid(affectedCell))
				{
					return true;
				}
			}

			return false;
		}
		return true;
	}

	/// <summary>
	/// 在指定坐标开始一次绘制操作
	/// </summary>
	public void DrawAt(Vector2I cellPos)
	{
		if (groundNode.BrushCursor == null) return;
		Vector2I[] affected = groundNode.BrushCursor.GetAffectedCells(cellPos.X, cellPos.Y);
		if (affected.Length > 0)
		{
			Godot.Collections.Array<Vector2I> godotAffected = new();
			foreach (Vector2I cell in affected)
			{
				if (!groundNode.InteractionNode.IsWithinBounds(cell))
				{
					continue;
				}

				if (IsOverlayMode && SelectedOverlay != OverlayType.Enums.NONE && !isOverlayPlacementValid(cell))
				{
					continue;
				}

				godotAffected.Add(cell);

				if (IsBuildingEditorActive)
				{
					placeBuilding(cell, CurrentSelectedBuilding);
				}
				else
				{
					clearBuildingsAt(cell);

					if (IsUpdatingGround && groundNode.Ground != null)
					{
						if (IsOverlayMode)
						{
							groundNode.Ground.SetGridOverlayData(cell.X, cell.Y, SelectedOverlay);
						}
						else
						{
							groundNode.Ground.SetGridData(cell.X, cell.Y, SelectedSurface, SelectedOverlay, SelectedHeight);
						}
					}
				}
			}

			if (godotAffected.Count > 0)
			{
				groundNode.EmitCellsDrawn(godotAffected);
			}
		}
	}

	private bool isOverlayPlacementValid(Vector2I cellPos)
	{
		Grid? grid = groundNode.World?.Ground?.GetGrid(cellPos.X, cellPos.Y);
		if (grid == null)
		{
			return false;
		}

		return Overlay.IsOverlayValid(grid.Surface, SelectedOverlay);
	}

	/// <summary>
	/// 放置建筑
	/// </summary>
	/// <param name="pos">地格坐标</param>
	/// <param name="templateName">建筑模板名称</param>
	private void placeBuilding(Vector2I pos, string templateName)
	{
		if (groundNode.World == null || groundNode.Ground == null) return;
		if (string.IsNullOrWhiteSpace(templateName)) return;
		if (!BuildingPlacementRule.CanPlaceBuilding(groundNode.World, pos))
		{
			return;
		}

		// 获取模板并进行调度
		BuildingTemplate template = BuildingTemplate.GetTemplate(templateName);

		// 边界检查：目前固定 1x1
		if (pos.X < 0 || pos.X >= groundNode.World.Ground.Width || pos.Y < 0 || pos.Y >= groundNode.World.Ground.Height) {
			return;
        }

		// 分类分发到不同系统的 AddBuilding 方法
		Country country = resolveCountry();

		if (template is HarvestBuildingTemplate ht)
		{
			HarvestBuilding? building = groundNode.World.Buildings.AddBuilding(pos, ht.TypeId, country);
			completeConstructionImmediately(building);
		}
		else if (template is IndustryBuildingTemplate it)
		{
			IndustryBuilding? building = groundNode.World.Buildings.AddBuilding(pos, it.TypeId, country);
			completeConstructionImmediately(building);
		}
		else if (template is ResidentialBuildingTemplate rt)
		{
			ResidentialBuilding? building = groundNode.World.Buildings.AddBuilding(pos, rt.TypeId, country);
			completeConstructionImmediately(building);
		}
		else if (template is MarketBuildingTemplate mt)
		{
			MarketBuilding? building = groundNode.World.Buildings.AddBuilding(pos, mt.TypeId, country);
			completeConstructionImmediately(building);
		}
	}

	private Country resolveCountry()
	{
		if (groundNode.World == null)
		{
			throw new System.InvalidOperationException("GroundEditorNode requires world reference.");
		}

		if (CurrentSelectedCountryId >= 0)
		{
			Country? selectedCountry = groundNode.World.CountryCollection.GetById(CurrentSelectedCountryId);
			if (selectedCountry != null)
			{
				return selectedCountry;
			}
		}

		System.Collections.Generic.List<Country> countries = groundNode.World.CountryCollection.GetAll();
		if (countries.Count == 0)
		{
			throw new System.InvalidOperationException("Building placement failed: country collection is empty.");
		}

		return countries[0];
	}

	private void completeConstructionImmediately(Building? building)
	{
		if (building == null)
		{
			return;
		}

		building.Construction.MarkCompleted();
	}

	/// <summary>
	/// 在指定坐标清理所有类型的建筑
	/// </summary>
	private void clearBuildingsAt(Vector2I cell)
	{
		if (groundNode.World == null) return;
		groundNode.World.Buildings.RemoveBuilding(cell);
	}
}
