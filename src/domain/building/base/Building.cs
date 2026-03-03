using Godot;
using System.Collections.Generic;

/// <summary>
/// 地图上建筑物，具有建造消耗和建造状态
/// </summary>
public abstract class Building : IIdModel, IInfo, IPeristenceRead
{
	private static IdAllocator idAllocator = new IdAllocator();


	/// <summary>
	/// 建筑模板，由子类提供。
	/// </summary>
	protected abstract BuildingTemplate Template { get; }

	/// <summary>
	/// 建筑种类标识字符串，用于统一存档。
	/// </summary>
	public string BuildingKind => Template.Kind;

	/// <summary>
	/// 建筑类型枚举值，用于持久化
	/// </summary>
	public int TypeIdValue => Template.TypeIdValue;

	/// <summary>
	/// 建筑类型名称，用于持久化
	/// </summary>
	public string TypeName => Template.TypeName;

	/// <summary>
	/// 名称
	/// </summary>
	public string Name => Template.Name;

	/// <summary>
	/// 建筑的渲染颜色，默认来自建筑模板
	/// </summary>
	public Color Color => Template.Color;






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
	/// 初始化建筑
	/// </summary>
	/// <param name="name">建筑名称</param>
	/// <param name="pos">所在位置（地格坐标）</param>
	/// <param name="country">所属国家</param>
	/// <param name="constructionCost">建造成本</param>
	/// <param name="processing">加工或采集模块（可为空）</param>
	protected Building(Vector2I pos, Country country, int? id = null)
	{
		Id = idAllocator.AllocateId(id);
		Country = country;
		Collision = new AtomCollision(pos);
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

		return data;
	}

	/// <summary>
	/// 获取建筑的持久化数据
	/// </summary>
	public virtual Dictionary<string, object> GetSaveData()
	{
		return new Dictionary<string, object>
		{
			{ "pos_x", Collision.Center.X },
			{ "pos_y", Collision.Center.Y },
			{ "type_id", TypeIdValue },
			{ "type_name", TypeName },
			{ "country_id", Country.Id },
		};
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
