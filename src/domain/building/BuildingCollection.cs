using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// 统一建筑集合，负责管理世界中所有建筑对象。
/// </summary>
public class BuildingCollection : DictCollection<Vector2I, Building>, ICollectionPersistence
{
	private readonly Ground ground;
	private readonly CountryCollection countryCollection;
	private readonly PopulationCollection populationCollection;

	private readonly Dictionary<int, Building> idToItem = new();

	// TODO 这是啥玩意啊？感觉不太对，为什么要在这里注册反序列化工厂？应该是每个建筑类自己注册吧？先放这儿，后续再重构。
	private static readonly Dictionary<BuildingType.Enums, Func<Dictionary<string, object>, Building.PersistenceContext, Building>> buildingLoaders = new();

	/// <summary>
	/// 构造函数，注入建筑系统依赖。
	/// </summary>
	public BuildingCollection(Ground ground, CountryCollection countryCollection, PopulationCollection populationCollection)
	{
		this.ground = ground;
		this.countryCollection = countryCollection;
		this.populationCollection = populationCollection;
	}

	/// <summary>
	/// 获取建筑键值（地格坐标）。
	/// </summary>
	protected override Vector2I GetKey(Building item) => item.Collision.Center;

	public override void Add(Building item)
	{
		base.Add(item);
		idToItem[item.Id] = item;
	}

	public override void Remove(Building item)
	{
		base.Remove(item);
		idToItem.Remove(item.Id);
	}

	public override void Clear()
	{
		base.Clear();
		idToItem.Clear();
	}



	public Building GetById(int id)
	{
		return idToItem[id];
	}

	/// <summary>
	/// 注册指定建筑类型的反序列化工厂。
	/// </summary>
	public static void RegisterLoader(BuildingType.Enums type, Func<Dictionary<string, object>, Building.PersistenceContext, Building> loader)
	{
		buildingLoaders[type] = loader;
	}


	/// <summary>
	/// 导出建筑存档数据。
	/// </summary>
	public List<Dictionary<string, object>> GetSaveData()
	{
		return GetAll().Select(building => building.GetSaveData()).ToList();
	}

	/// <summary>
	/// 从统一存档数据加载建筑。
	/// </summary>
	public void LoadSaveData(List<Dictionary<string, object>> data)
	{
		Clear();
		data
			.Select(loadBuilding)
			.ToList()
			.ForEach(Add);
	}

	private Building loadBuilding(Dictionary<string, object> data)
	{
		BuildingType.Enums type = parseBuildingType(data);
		Building.PersistenceContext context = new(countryCollection, populationCollection);

		if (buildingLoaders.ContainsKey(type))
		{
			return buildingLoaders[type](data, context);
		}

		return Building.LoadSaveData(data, context);
	}

	private static BuildingType.Enums parseBuildingType(Dictionary<string, object> data)
	{
		string typeName = data.ContainsKey("type_name") ? data["type_name"].ToString() ?? string.Empty : string.Empty;
		if (!string.IsNullOrEmpty(typeName) && Enum.TryParse(typeName, true, out BuildingType.Enums typeByName))
		{
			return typeByName;
		}

		return (BuildingType.Enums)Convert.ToInt32(data["type_id"]);
	}


}
