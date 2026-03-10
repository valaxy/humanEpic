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
		saveData["buildings"] = DomainModelJsonPersistence.SaveToObjectWithoutStatic(Buildings, new object[] { Countries, Populations });
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
			{ "countries", DomainModelJsonPersistence.SaveToObjectWithoutStatic(Countries) },
			{ "populations", DomainModelJsonPersistence.SaveToObjectWithoutStatic(Populations) }
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
		GD.Print($"[Load] GameWorld.LoadSaveData started. Root keys: {string.Join(", ", data.Keys.OrderBy(key => key))}");
		applyRootStaticMembers(data);
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
		GD.Print($"[Load] GameWorld ready. countries={countries.Size}, populations={populations.Size}, buildings={buildings.Size}");
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
		if (!data.TryGetValue("countries", out object? countriesNodeRaw))
		{
			return new CountryCollection();
		}

		if (countriesNodeRaw is not Dictionary<string, object> countriesNode)
		{
			throw new System.InvalidOperationException("countries 结构非法");
		}

		return DomainModelJsonPersistence.LoadFromObjectWithoutStatic<CountryCollection>(countriesNode);
	}

	private static PopulationCollection loadPopulations(Dictionary<string, object> data)
	{
		if (!data.TryGetValue("populations", out object? populationsNodeRaw))
		{
			return new PopulationCollection();
		}

		if (populationsNodeRaw is not Dictionary<string, object> populationsNode)
		{
			throw new System.InvalidOperationException("populations 结构非法");
		}

		return DomainModelJsonPersistence.LoadFromObjectWithoutStatic<PopulationCollection>(populationsNode);
	}

	private static BuildingCollection loadBuildings(Dictionary<string, object> data, CountryCollection countries, PopulationCollection populations)
	{
		object[] entityCollections = { countries, populations };
		if (!data.TryGetValue("buildings", out object? buildingsNodeRaw))
		{
			GD.Print("[Load] buildings key not found, using empty BuildingCollection.");
			return new BuildingCollection();
		}

		if (buildingsNodeRaw is List<object> listNode && listNode.Count == 0)
		{
			GD.Print("[Load] buildings is an empty array (legacy/abnormal shape), fallback to empty BuildingCollection.");
			return new BuildingCollection();
		}

		if (buildingsNodeRaw is not Dictionary<string, object> buildingsNode)
		{
			string rawType = buildingsNodeRaw?.GetType().FullName ?? "null";
			GD.PushError($"[Load] buildings node type invalid: {rawType}");
			throw new System.InvalidOperationException("buildings 结构非法");
		}

		return DomainModelJsonPersistence.LoadFromObjectWithoutStatic<BuildingCollection>(buildingsNode, entityCollections);
	}

	private static void applyRootStaticMembers(Dictionary<string, object> data)
	{
		Dictionary<string, object> staticNode = DomainModelJsonPersistence.ExtractStaticMembers(data);
		if (staticNode.Count == 0)
		{
			return;
		}

		DomainModelJsonPersistence.ApplyStaticMembers(staticNode);
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