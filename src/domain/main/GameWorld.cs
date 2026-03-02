using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 领域层顶层入口类，负责所有领域对象的实例化、生命周期管理和依赖注入。
/// GDScript 只需要持有此类的引用即可访问整个领域层。
/// TODO，没改完
/// </summary>
[GlobalClass]
public partial class GameWorld : RefCounted
{
	private const float buildingCountryMarkRadius = 3.0f;
	private static bool templatesInitialized = false;

	public Ground Ground { get; private set; }
	public TimeSystem TimeSystem { get; private set; }
	// public CountryCollection CountryCollection { get; private set; }

	// public PopulationCollection PopulationCollection { get; private set; }
	// public UnitCollection UnitCollection { get; private set; }
	// public WildlifeCollection WildlifeCollection { get; private set; }
	// public BuildingCollection Buildings { get; private set; }
	// public NaturalDisasterCollection NaturalDisasters { get; private set; }
	// public Simulation Simulation { get; private set; }

	public GameWorld()
	{
		ulong start = Time.GetTicksMsec();

		ensureAllTemplatesLoaded();

		Ground = new Ground();
		TimeSystem = new TimeSystem();
		// CountryCollection = CountryCollection.Instance;
		// GameWorldDataInitializer.InitializeCountryCollection(CountryCollection);

		// PopulationCollection = new PopulationCollection();
		// UnitCollection = new UnitCollection(Ground, PopulationCollection, CountryCollection);

		// WildlifeCollection = new WildlifeCollection();
		// Buildings = new BuildingCollection(Ground, PopulationCollection, CountryCollection);
		// NaturalDisasters = new NaturalDisasterCollection();

		// Simulation = new Simulation(
		// 	this,
		// 	Ground,
		// 	Buildings,
		// 	WildlifeCollection,
		// 	UnitCollection,
		// 	NaturalDisasters,
		// 	TimeSystem
		// );

		// bindBuildingTerritoryRefreshEvents();
		// refreshBuildingTerritoryColors();
		GD.Print($"[Perf] GameWorld init took {Time.GetTicksMsec() - start} ms");
	}

	private static void ensureAllTemplatesLoaded()
	{
		if (templatesInitialized)
		{
			return;
		}

		// ConstructionCostParser.Initialize();
		// ProcessingParser.Initialize();
		// HarvestBuildingTemplate.Initialize();
		// IndustryBuildingTemplate.Initialize();
		// ResidentialBuildingTemplate.Initialize();
		// MarketBuildingTemplate.Initialize();
		// _ = ProductTemplate.GetTemplates().Count;

		templatesInitialized = true;
	}

	/// <summary>
	/// 保存游戏状态
	/// </summary>
	public void Save()
	{
		ulong start = Time.GetTicksMsec();
		// RebuildPopulationCollection();
		// Persistence.SaveGame(this);
		GD.Print($"[Perf] GameWorld save took {Time.GetTicksMsec() - start} ms");
	}

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

	/// <summary>
	/// 加载游戏状态
	/// </summary>
	public void Load()
	{
		// Persistence.LoadGame(this);
		// GameWorldDataInitializer.EnsureCountryCollection(this);
		// GameWorldDataInitializer.EnsureColdStart(this);
		// refreshBuildingTerritoryColors();
	}

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

}
