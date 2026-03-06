using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// 地图上建筑物，具有建造消耗和建造状态
/// </summary>
public class Building : IIdModel, IInfo, IPersistence<Building, Building.PersistenceContext>
{
	private static IdAllocator idAllocator = new IdAllocator();

	/// <summary>
	/// 建筑反序列化上下文。
	/// </summary>
	public readonly struct PersistenceContext
	{
		/// <summary>
		/// 国家集合上下文。
		/// </summary>
		public CountryCollection CountryCollection { get; }

		/// <summary>
		/// 人口集合上下文。
		/// </summary>
		public PopulationCollection PopulationCollection { get; }

		/// <summary>
		/// 构造上下文。
		/// </summary>
		public PersistenceContext(CountryCollection countryCollection, PopulationCollection populationCollection)
		{
			CountryCollection = countryCollection;
			PopulationCollection = populationCollection;
		}
	}


	// 建筑模板，由子类提供。
	private BuildingTemplate template { get; }

	/// <summary>
	/// 建筑类型枚举值，来自模板
	/// </summary>
	public BuildingType.Enums TypeId => template.TypeId;


	/// <summary>
	/// 名称
	/// </summary>
	public string Name => template.Name;

	/// <summary>
	/// 建筑的渲染颜色，默认来自建筑模板
	/// </summary>
	public Color Color => template.Color;

	/// <summary>
	/// 建筑唯一标识
	/// </summary>
	public int Id { get; }

	/// <summary>
	/// 所属国家
	/// </summary>
	public Country Country { get; set; }






	/// <summary>
	/// 碰撞信息（总是占用 1x1 地格）
	/// </summary>
	public AtomCollision Collision { get; }

	/// <summary>
	/// 民宅建筑的居住功能组件，可以没有
	/// </summary>
	public Residential? Residential { get; private set; }

	/// <summary>
	/// 市场建筑的市场功能组件，可以没有
	/// </summary>
	public Market? Market { get; private set; }

	/// <summary>
	/// 建筑仓库（所有建筑固定拥有）。
	/// </summary>
	public Warehouse Warehouse { get; private set; }




	/// <summary>
	/// 初始化建筑
	/// </summary>
	/// <param name="name">建筑名称</param>
	/// <param name="pos">所在位置（地格坐标）</param>
	/// <param name="country">所属国家</param>
	/// <param name="constructionCost">建造成本</param>
	/// <param name="processing">加工或采集模块（可为空）</param>
	public Building(BuildingTemplate template, Vector2I pos, Country country, int? id = null)
	{
		this.template = template;
		Id = idAllocator.AllocateId(id);
		Country = country;
		Collision = new AtomCollision(pos);
		Warehouse = new Warehouse(1000.0f);
		Residential = ResidentialTemplate.HasTemplate(template.TypeId)
			? new Residential(ResidentialTemplate.GetTemplate(template.TypeId).MaxPopulation)
			: null;
		Market = template.TypeId == BuildingType.Enums.Market
			? new global::Market()
			: null;
	}



	/// <summary>
	/// 获取用于 UI 展示的建筑信息
	/// </summary>
	/// <returns>分层的键值对字典</returns>
	public virtual InfoData GetInfoData()
	{
		InfoData basicInfoNode = new();
		basicInfoNode.AddText("名称", Name);
		basicInfoNode.AddText("所属国家", Country.Name);

		InfoData data = new();
		data.AddGroup("基本信息", basicInfoNode);

		if (Residential != null)
		{
			data.AddGroup("居住信息", Residential.GetInfoData());
		}

		if (Market != null)
		{
			data.AddGroup("市场信息", Market.GetInfoData());
		}

		data.AddGroup("仓库信息", Warehouse.GetInfoData());

		return data;
	}

	/// <summary>
	/// 获取建筑的持久化数据
	/// </summary>
	public virtual Dictionary<string, object> GetSaveData()
	{
		Dictionary<string, object> saveData = new Dictionary<string, object>
		{
			{ "id", Id },
			{ "pos_x", Collision.Center.X },
			{ "pos_y", Collision.Center.Y },
			{ "type_id", (int)TypeId },
			{ "type_name", TypeId.ToString() },
			{ "country_id", Country.Id },
		};

		if (Residential != null)
		{
			saveData["residential"] = Residential.GetSaveData();
		}

		if (Market != null)
		{
			saveData["market"] = Market.GetSaveData();
		}

		saveData["warehouse"] = Warehouse.GetSaveData();

		return saveData;
	}

	/// <summary>
	/// 从持久化数据恢复一个基础建筑实例。
	/// </summary>
	public static Building LoadSaveData(Dictionary<string, object> data, PersistenceContext context = default)
	{
		Debug.Assert(context.CountryCollection != null, "恢复 Building 需要 CountryCollection 上下文");
		Debug.Assert(context.PopulationCollection != null, "恢复 Building 需要 PopulationCollection 上下文");
		PersistenceContext persistenceContext = context;

		int id = Convert.ToInt32(data["id"]);
		int posX = Convert.ToInt32(data["pos_x"]);
		int posY = Convert.ToInt32(data["pos_y"]);
		int countryId = Convert.ToInt32(data["country_id"]);

		BuildingTemplate template = loadTemplate(data);
		Country country = persistenceContext.CountryCollection.Get(countryId);
		Building building = new Building(template, new Vector2I(posX, posY), country, id);

		if (data.ContainsKey("residential"))
		{
			Dictionary<string, object> residentialData = (Dictionary<string, object>)data["residential"];
			building.Residential = Residential.LoadSaveData(residentialData, persistenceContext.PopulationCollection);
		}

		if (data.ContainsKey("market"))
		{
			Dictionary<string, object> marketData = (Dictionary<string, object>)data["market"];
			building.Market = global::Market.LoadSaveData(marketData);
		}

		if (data.ContainsKey("warehouse"))
		{
			Dictionary<string, object> warehouseData = (Dictionary<string, object>)data["warehouse"];
			building.Warehouse = Warehouse.LoadSaveData(warehouseData);
		}

		return building;
	}

	private static BuildingTemplate loadTemplate(Dictionary<string, object> data)
	{
		string typeName = data.ContainsKey("type_name") ? data["type_name"].ToString() ?? string.Empty : string.Empty;
		if (!string.IsNullOrEmpty(typeName) && Enum.TryParse(typeName, true, out BuildingType.Enums parsedTypeByName))
		{
			return BuildingTemplate.GetTemplate(parsedTypeByName);
		}

		int typeId = Convert.ToInt32(data["type_id"]);
		BuildingType.Enums parsedTypeById = (BuildingType.Enums)typeId;
		return BuildingTemplate.GetTemplate(parsedTypeById);
	}
}



// /// <summary>
// /// 从建筑模板构建建造成本集合。
// /// </summary>
// protected static ProductCapacityCollection CreateConstructionCostCollection(BuildingTemplate template)
// {
// 	return new ProductCapacityCollection(new Dictionary<ProductType.Enums, int>(template.ConstructionCost));
// }


// Warehouse = new Warehouse();
// Warehouse.Changed += () => EmitSignal(SignalName.StorageChanged);
// Construction = new Construction(constructionCost);
// Processing = processing;
// Workforce = new Workforce(this);

// if (Construction.IsUnderConstruction)
// {
// 	data.AddGroup("建造详情", Construction.GetInfoData());
// }

// InfoData warehouseInfo = Warehouse.GetInfoData();
// if (!warehouseInfo.IsEmpty)
// {
// 	data.AddGroup("仓库信息", warehouseInfo);
// }

// InfoData workforceInfo = Workforce.GetInfoData();
// if (!workforceInfo.IsEmpty)
// {
// 	data.AddGroup("劳动力信息", workforceInfo);
// }

// { "construction", Construction.GetSaveData() },
// { "warehouse", Warehouse.GetSaveData() },

// /// <summary>
// /// 仓储变更信号
// /// </summary>
// [Signal]
// public delegate void StorageChangedEventHandler();



// /// <summary>
// /// 建筑仓储
// /// </summary>
// public Warehouse Warehouse { get; }

// /// <summary>
// /// 建造模块
// /// </summary>
// public Construction Construction { get; }


// /// <summary>
// /// 建筑加工/采集模块，可为空
// /// </summary>
// public Processing? Processing { get; }

// /// <summary>
// /// 建筑工人分配情况
// /// </summary>
// public Workforce Workforce { get; protected set; }



// /// <summary>
// /// 需要外部注入
// /// </summary>
// public static IBuildingLifecycle Lifecycle { get; set; } = null!;

// public IBuildingLifecycle LifecycleInstance => Lifecycle;
