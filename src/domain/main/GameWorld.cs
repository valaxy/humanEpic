using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 领域层顶层入口类，负责所有领域对象的实例化、生命周期管理和依赖注入。
/// 只需要持有此类的引用即可访问整个领域层。
/// </summary>
[GlobalClass]
public partial class GameWorld : RefCounted, IPersistence<GameWorld>
{
	public Ground Ground { get; private set; } = null!;
	public TimeSystem TimeSystem { get; private set; } = null!;
	public CountryCollection Countries { get; private set; } = null!;
	public PopulationCollection Populations { get; private set; } = null!;
	public BuildingCollection Buildings { get; private set; } = null!;

	private GameWorld() { }

	/// <summary>
	/// 获取世界对象的可持久化数据。
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		Dictionary<string, object> saveData = Ground.GetSaveData();
		saveData["time"] = TimeSystem.TotalSeconds;
		saveData["countries"] = Countries.GetSaveData();
		saveData["populations"] = Populations.GetSaveData();
		saveData["buildings"] = Buildings.GetSaveData();
		return saveData;
	}


	/// <summary>
	/// 通过持久化数据创建并恢复世界对象。
	/// </summary>
	public static GameWorld LoadSaveData(Dictionary<string, object> data)
	{
		Ground ground = Ground.LoadSaveData(data);
		CountryCollection countries = loadCountries(data);
		PopulationCollection populations = loadPopulations(data);
		BuildingCollection buildings = new BuildingCollection(ground, countries, populations);
		loadBuildings(data, buildings);

		GameWorld world = new GameWorld
		{
			Ground = ground,
			TimeSystem = TimeSystem.LoadSaveData(data),
			Countries = countries,
			Populations = populations,
			Buildings = buildings,
		};
		return world;
	}

	private static CountryCollection loadCountries(Dictionary<string, object> data)
	{
		CountryCollection countries = new CountryCollection();
		if (data.ContainsKey("countries"))
		{
			List<Dictionary<string, object>> savedCountries = ((List<object>)data["countries"])
				.Select(item => (Dictionary<string, object>)item)
				.ToList();
			countries.LoadSaveData(savedCountries);
			return countries;
		}

		countries.Add(new Country("默认国家", Colors.CornflowerBlue));
		return countries;
	}

	private static PopulationCollection loadPopulations(Dictionary<string, object> data)
	{
		PopulationCollection populations = new PopulationCollection();
		if (data.ContainsKey("populations"))
		{
			List<Dictionary<string, object>> savedPopulations = ((List<object>)data["populations"])
				.Select(item => (Dictionary<string, object>)item)
				.ToList();
			populations.LoadSaveData(savedPopulations);
		}

		return populations;
	}

	private static void loadBuildings(Dictionary<string, object> data, BuildingCollection buildings)
	{
		if (!data.ContainsKey("buildings"))
		{
			return;
		}

		List<Dictionary<string, object>> savedBuildings = ((List<object>)data["buildings"])
			.Select(item => (Dictionary<string, object>)item)
			.ToList();
		buildings.LoadSaveData(savedBuildings);
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