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
	/// 获取地图内容的可持久化数据。
	/// </summary>
	public Dictionary<string, object> GetMapSaveData()
	{
		Dictionary<string, object> saveData = Ground.GetSaveData();
		saveData["buildings_node"] = DomainModelJsonPersistence.SaveToObject(Buildings, new object[] { Countries, Populations });
		return saveData;
	}

	/// <summary>
	/// 获取非地图内容的可持久化数据。
	/// </summary>
	public Dictionary<string, object> GetWorldStateSaveData()
	{
		return new Dictionary<string, object>
		{
			{ "time", TimeSystem.TotalSeconds },
			{ "countries_node", DomainModelJsonPersistence.SaveToObject(Countries) },
			{ "populations_node", DomainModelJsonPersistence.SaveToObject(Populations) }
		};
	}

	/// <summary>
	/// 获取完整世界对象的可持久化数据（兼容旧存档流程）。
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		return GetMapSaveData()
			.Concat(GetWorldStateSaveData())
			.ToDictionary(pair => pair.Key, pair => pair.Value);
	}


	/// <summary>
	/// 通过持久化数据创建并恢复世界对象。
	/// </summary>
	public static GameWorld LoadSaveData(Dictionary<string, object> data)
	{
		Ground ground = Ground.LoadSaveData(data);
		CountryCollection countries = loadCountries(data);
		PopulationCollection populations = loadPopulations(data);
		BuildingCollection buildings = loadBuildings(data, countries, populations);

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

	/// <summary>
	/// 通过地图数据与世界状态数据创建并恢复世界对象。
	/// </summary>
	public static GameWorld LoadSaveData(Dictionary<string, object> mapData, Dictionary<string, object> worldStateData)
	{
		Dictionary<string, object> mergedData = mapData
			.Concat(worldStateData)
			.GroupBy(pair => pair.Key)
			.ToDictionary(group => group.Key, group => group.Last().Value);
		return LoadSaveData(mergedData);
	}

	private static CountryCollection loadCountries(Dictionary<string, object> data)
	{
		if (data.TryGetValue("countries_node", out object? countriesNodeRaw))
		{
			if (countriesNodeRaw is not Dictionary<string, object> countriesNode)
			{
				throw new System.InvalidOperationException("countries_node 结构非法");
			}

			return DomainModelJsonPersistence.LoadFromObject<CountryCollection>(countriesNode);
		}

		if (!data.ContainsKey("countries"))
		{
			return new CountryCollection();
		}

		Dictionary<string, object> node = new Dictionary<string, object>
		{
			{ "items", data["countries"] }
		};
		return DomainModelJsonPersistence.LoadFromObject<CountryCollection>(node);
	}

	private static PopulationCollection loadPopulations(Dictionary<string, object> data)
	{
		if (data.TryGetValue("populations_node", out object? populationsNodeRaw))
		{
			if (populationsNodeRaw is not Dictionary<string, object> populationsNode)
			{
				throw new System.InvalidOperationException("populations_node 结构非法");
			}

			return DomainModelJsonPersistence.LoadFromObject<PopulationCollection>(populationsNode);
		}

		if (!data.ContainsKey("populations"))
		{
			return new PopulationCollection();
		}

		Dictionary<string, object> node = new Dictionary<string, object>
		{
			{ "items", data["populations"] }
		};
		return DomainModelJsonPersistence.LoadFromObject<PopulationCollection>(node);
	}

	private static BuildingCollection loadBuildings(Dictionary<string, object> data, CountryCollection countries, PopulationCollection populations)
	{
		object[] entityCollections = { countries, populations };

		if (data.TryGetValue("buildings_node", out object? buildingsNodeRaw))
		{
			if (buildingsNodeRaw is not Dictionary<string, object> buildingsNode)
			{
				throw new System.InvalidOperationException("buildings_node 结构非法");
			}

			return DomainModelJsonPersistence.LoadFromObject<BuildingCollection>(buildingsNode, entityCollections);
		}

		if (!data.ContainsKey("buildings"))
		{
			return new BuildingCollection();
		}

		BuildingCollection buildings = new BuildingCollection();
		((List<object>)data["buildings"])
			.Select(item => (Dictionary<string, object>)item)
			.Select(item => DomainModelJsonPersistence.LoadFromObjectAsCollectionItem<Building>(
				item,
				typeof(BuildingCollection),
				entityCollections))
			.ToList()
			.ForEach(buildings.Add);
		return buildings;
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