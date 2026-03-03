using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// 统一建筑集合，负责管理世界中所有建筑对象。
/// </summary>
[GlobalClass]
public partial class BuildingCollection : DictCollection<Vector2I, Building>, ICollectionPersistence
{
	private readonly Ground ground;
	private readonly CountryCollection countryCollection;
	private readonly PopulationCollection populationCollection;
	private readonly HarvestBuildingLifecycle harvestLifecycle;
	private readonly IndustryBuildingLifecycle industryLifecycle;
	private readonly ResidentialBuildingLifecycle residentialLifecycle;
	private readonly MarketBuildingLifecycle marketLifecycle;

	/// <summary>
	/// 提供对底层字典的访问，用于兼容现有调用方式。
	/// </summary>
	public Dictionary<Vector2I, Building> Buildings => items;

	/// <summary>
	/// 构造函数，注入建筑系统依赖。
	/// </summary>
	public BuildingCollection(Ground ground, PopulationCollection populationCollection, CountryCollection countryCollection)
	{
		this.ground = ground;
		this.populationCollection = populationCollection;
		this.countryCollection = countryCollection;
		harvestLifecycle = new HarvestBuildingLifecycle();
		industryLifecycle = new IndustryBuildingLifecycle();
		residentialLifecycle = new ResidentialBuildingLifecycle(populationCollection);
		marketLifecycle = new MarketBuildingLifecycle();

		HarvestBuilding.Lifecycle = harvestLifecycle;
		IndustryBuilding.Lifecycle = industryLifecycle;
		ResidentialBuilding.Lifecycle = residentialLifecycle;
		MarketBuilding.Lifecycle = marketLifecycle;
	}

	/// <summary>
	/// 关联的地理数据。
	/// </summary>
	public Ground Ground => ground;

	/// <summary>
	/// 获取建筑键值（地格坐标）。
	/// </summary>
	protected override Vector2I GetKey(Building item)
	{
		return item.Collision.Center;
	}

	/// <summary>
	/// 添加建筑并触发生命周期。
	/// </summary>
	public override void Add(Building building)
	{
		if (HasKey(building.Collision.Center))
		{
			return;
		}

		base.Add(building);
		building.LifecycleInstance.OnAdd(building);
	}

	/// <summary>
	/// 移除建筑并触发生命周期。
	/// </summary>
	public override void Remove(Building item)
	{
		invokeLifecycleOnRemove(item);
		base.Remove(item);
	}

	/// <summary>
	/// 新增采集建筑。
	/// </summary>
	public HarvestBuilding? AddBuilding(Vector2I pos, HarvestBuildingType.Enums templateType, Country? country = null)
	{
		return addBuilding<HarvestBuilding, HarvestBuildingType.Enums>(pos, templateType, harvestLifecycle, country);
	}

	/// <summary>
	/// 新增工业建筑。
	/// </summary>
	public IndustryBuilding? AddBuilding(Vector2I pos, IndustryBuildingType.Enums templateType, Country? country = null)
	{
		return addBuilding<IndustryBuilding, IndustryBuildingType.Enums>(pos, templateType, industryLifecycle, country);
	}

	/// <summary>
	/// 新增民宅建筑。
	/// </summary>
	public ResidentialBuilding? AddBuilding(Vector2I pos, ResidentialBuildingType.Enums templateType, Country? country = null)
	{
		return addBuilding<ResidentialBuilding, ResidentialBuildingType.Enums>(pos, templateType, residentialLifecycle, country);
	}

	/// <summary>
	/// 新增市场建筑。
	/// </summary>
	public MarketBuilding? AddBuilding(Vector2I pos, MarketBuildingType.Enums templateType, Country? country = null)
	{
		return addBuilding<MarketBuilding, MarketBuildingType.Enums>(pos, templateType, marketLifecycle, country);
	}

	/// <summary>
	/// 获取指定地格上的建筑。
	/// </summary>
	public Building? GetBuilding(Vector2I pos)
	{
		return Get(pos);
	}

	public Building GetById(int id)
	{
		foreach (Building building in items.Values)
		{
			if (building.Id == id)
			{
				return building;
			}
		}

		throw new InvalidOperationException($"无法从 id={id} 解析建筑对象。");
	}

	/// <summary>
	/// 获取指定地格上的指定类型建筑。
	/// </summary>
	public TBuilding? GetBuilding<TBuilding>(Vector2I pos) where TBuilding : Building
	{
		Building? building = Get(pos);
		if (building is TBuilding typedBuilding)
		{
			return typedBuilding;
		}

		return null;
	}

	/// <summary>
	/// 获取所有指定类型建筑。
	/// </summary>
	public List<TBuilding> GetBuildings<TBuilding>() where TBuilding : Building
	{
		List<TBuilding> result = new();
		foreach (Building building in items.Values)
		{
			if (building is TBuilding typedBuilding)
			{
				result.Add(typedBuilding);
			}
		}

		return result;
	}

	/// <summary>
	/// 移除指定地格上的建筑。
	/// </summary>
	public void RemoveBuilding(Vector2I pos)
	{
		Building? building = Get(pos);
		if (building != null)
		{
			Remove(building);
		}
	}

	/// <summary>
	/// 导出建筑存档数据。
	/// </summary>
	public List<Dictionary<string, object>> GetSaveData()
	{
		List<Dictionary<string, object>> data = new();
		foreach (Building building in items.Values)
		{
			Dictionary<string, object> buildingData = building.GetSaveData();
			buildingData["building_kind"] = building.BuildingKind;
			data.Add(buildingData);
		}

		return data;
	}

	/// <summary>
	/// 从统一存档数据加载建筑。
	/// </summary>
	public void LoadSaveData(List<Dictionary<string, object>> data)
	{
		Clear();
		foreach (Dictionary<string, object> dict in data)
		{
			Building building = loadBuilding(dict);
			Add(building);
		}
	}

	/// <summary>
	/// 从旧版民宅分片数据加载。
	/// </summary>
	public void LoadResidentialSaveData(List<Dictionary<string, object>> data)
	{
		foreach (Dictionary<string, object> dict in data)
		{
			Country country = resolveCountryFromContext(extractCountryId(dict));
			Add(ResidentialBuilding.LoadSaveData(dict, country, populationCollection));
		}
	}

	/// <summary>
	/// 从旧版采集分片数据加载。
	/// </summary>
	public void LoadHarvestSaveData(List<Dictionary<string, object>> data)
	{
		foreach (Dictionary<string, object> dict in data)
		{
			Country country = resolveCountryFromContext(extractCountryId(dict));
			Add(HarvestBuilding.LoadSaveData(dict, country));
		}
	}

	/// <summary>
	/// 从旧版工业分片数据加载。
	/// </summary>
	public void LoadIndustrySaveData(List<Dictionary<string, object>> data)
	{
		foreach (Dictionary<string, object> dict in data)
		{
			Country country = resolveCountryFromContext(extractCountryId(dict));
			Add(IndustryBuilding.LoadSaveData(dict, country));
		}
	}

	/// <summary>
	/// 从旧版市场分片数据加载。
	/// </summary>
	public void LoadMarketSaveData(List<Dictionary<string, object>> data)
	{
		foreach (Dictionary<string, object> dict in data)
		{
			Country country = resolveCountryFromContext(extractCountryId(dict));
			Add(MarketBuilding.LoadSaveData(dict, country));
		}
	}

	private TBuilding? addBuilding<TBuilding, TEnum>(Vector2I pos, TEnum templateType, IBuildingLifecycle lifecycle, Country? country)
		where TBuilding : Building
		where TEnum : struct, Enum
	{
		Building? existingBuilding = Get(pos);
		if (existingBuilding is TBuilding existedTypedBuilding)
		{
			return existedTypedBuilding;
		}

		if (existingBuilding != null)
		{
			return null;
		}

		Country resolvedCountry = country ?? resolveCountryFromContext(null);
		int templateId = Convert.ToInt32(templateType);
		Building createdBuilding = lifecycle.OnCreate(pos, templateId, resolvedCountry);
		if (createdBuilding is not TBuilding typedBuilding)
		{
			return null;
		}

		Add(typedBuilding);
		return typedBuilding;
	}

	private void invokeLifecycleOnRemove(Building building)
	{
		building.LifecycleInstance.OnRemove(building);
	}

	private Building loadBuilding(Dictionary<string, object> data)
	{
		Country country = resolveCountryFromContext(extractCountryId(data));
		string buildingKind = extractBuildingKind(data);

		if (buildingKind == "residential")
		{
			return ResidentialBuilding.LoadSaveData(data, country, populationCollection);
		}

		if (buildingKind == "harvest")
		{
			return HarvestBuilding.LoadSaveData(data, country);
		}

		if (buildingKind == "industry")
		{
			return IndustryBuilding.LoadSaveData(data, country);
		}

		if (buildingKind == "market")
		{
			return MarketBuilding.LoadSaveData(data, country);
		}

		throw new InvalidOperationException($"Unknown building kind '{buildingKind}'.");
	}

	private string extractBuildingKind(Dictionary<string, object> data)
	{
		Debug.Assert(data.ContainsKey("building_kind"));
		return data["building_kind"].ToString()!;
	}

	private int? extractCountryId(Dictionary<string, object> data)
	{
		if (!data.ContainsKey("country_id"))
		{
			return null;
		}

		return Convert.ToInt32(data["country_id"]);
	}

	private Country resolveCountryFromContext(int? countryId)
	{
		if (countryId.HasValue)
		{
			Country? loadedCountry = countryCollection.GetById(countryId.Value);
			if (loadedCountry != null)
			{
				return loadedCountry;
			}

			throw new InvalidOperationException($"Building data error: unknown country_id '{countryId.Value}'.");
		}

		List<Country> countries = countryCollection.GetAll();
		if (countries.Count == 0)
		{
			throw new InvalidOperationException("Building data error: country collection is empty.");
		}

		return countries[0];
	}
}
