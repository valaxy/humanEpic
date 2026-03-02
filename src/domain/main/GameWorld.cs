using Godot;
using System.Collections.Generic;

/// <summary>
/// 领域层顶层入口类，负责所有领域对象的实例化、生命周期管理和依赖注入。
/// 只需要持有此类的引用即可访问整个领域层。
/// </summary>
[GlobalClass]
public partial class GameWorld : RefCounted, IPersistence<GameWorld>
{
	public Ground Ground { get; private set; } = null!;
	public TimeSystem TimeSystem { get; private set; } = null!;

	private GameWorld() { }

	/// <summary>
	/// 获取世界对象的可持久化数据。
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		Dictionary<string, object> saveData = Ground.GetSaveData();
		saveData["time"] = TimeSystem.TotalSeconds;
		return saveData;
	}


	/// <summary>
	/// 通过持久化数据创建并恢复世界对象。
	/// </summary>
	public static GameWorld LoadSaveData(Dictionary<string, object> data)
	{
		GameWorld world = new GameWorld
		{
			Ground = Ground.LoadSaveData(data),
			TimeSystem = TimeSystem.LoadSaveData(data),
		};
		return world;
	}
}



// private const float buildingCountryMarkRadius = 3.0f;

// public CountryCollection CountryCollection { get; private set; }

// public PopulationCollection PopulationCollection { get; private set; }
// public UnitCollection UnitCollection { get; private set; }
// public WildlifeCollection WildlifeCollection { get; private set; }
// public BuildingCollection Buildings { get; private set; }
// public NaturalDisasterCollection NaturalDisasters { get; private set; }
// public Simulation Simulation { get; private set; }

// /// <summary>
// /// 重建人口集合索引，供持久化使用
// /// </summary>
// public void RebuildPopulationCollection()
// {
// 	PopulationCollection.Clear();

// 	foreach (ResidentialBuilding building in Buildings.GetBuildings<ResidentialBuilding>())
// 	{
// 		PopulationCollection.Add(building.Population);
// 	}

// 	foreach (Unit unit in UnitCollection.GetAll())
// 	{
// 		PopulationCollection.Add(unit.Holds.Population);
// 	}

// 	foreach (Wildlife wildlife in WildlifeCollection.GetAll())
// 	{
// 		PopulationCollection.Add(wildlife.Holds.Population);
// 	}
// }


// private void bindBuildingTerritoryRefreshEvents()
// {
// 	Buildings.Added += _ => refreshBuildingTerritoryColors();
// 	Buildings.Removed += _ => refreshBuildingTerritoryColors();
// }

// /// <summary>
// /// 刷新所有建筑对地格的国家颜色标记
// /// </summary>
// public void RefreshBuildingTerritoryColors()
// {
// 	refreshBuildingTerritoryColors();
// }

// private void refreshBuildingTerritoryColors()
// {
// 	if (Ground.Width <= 0 || Ground.Height <= 0)
// 	{
// 		return;
// 	}

// 	for (int y = 0; y < Ground.Height; y++)
// 	{
// 		for (int x = 0; x < Ground.Width; x++)
// 		{
// 			Grid grid = Ground.GetGrid(x, y);
// 			grid.CountryColor = null;
// 		}
// 	}

// 	foreach (Building building in Buildings.GetAll())
// 	{
// 		applyBuildingCountryColor(building);
// 	}
// }

// private void applyBuildingCountryColor(Building building)
// {
// 	Vector2I centerGrid = building.Collision.Center;
// 	Vector2 center = new Vector2(centerGrid.X + 0.5f, centerGrid.Y + 0.5f);
// 	List<Vector2I> coveredCells = CircularGridTool.GetCellsByCenter(center, buildingCountryMarkRadius, Ground.Width, Ground.Height);

// 	foreach (Vector2I cellPos in coveredCells)
// 	{
// 		Grid grid = Ground.GetGrid(cellPos.X, cellPos.Y);
// 		grid.CountryColor = building.Country.Color;
// 	}
// }