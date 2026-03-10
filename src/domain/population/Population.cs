using System;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 人口类，代表了具有统一特征的一群人
/// - 人口是一种抽象的存在
/// - 人口具有三种功能：消费、劳动、投资
/// </summary>
[Persistable]
[PersistEntity(typeof(PopulationCollection))]
public class Population : IIdModel, IInfo
{
	[PersistField]
	private static int nextId = 1;

	[PersistField]
	private int id = default!;

	[PersistField]
	private string name = default!;

	[PersistField]
	private int count = default!;

	[PersistField]
	private int labourCount = 0; // TODO 不要在这里赋值

	[PersistField]
	private DemandCollection demands = default!;

	[PersistField]
	private Asset asset = default!;

	[PersistField]
	private PopulationResidential populationResidential = default!;


	/// <summary>
	/// 人口模型唯一标识（自增主键）
	/// </summary>

	public int Id => id;

	/// <summary>
	/// 人口名称（自然语言描述）
	/// </summary>
	public string Name => name;

	/// <summary>
	/// 人口人数
	/// </summary>
	public int Count => count;

	/// <summary>
	/// 已经工作的人数
	/// </summary>
	public int LabourCount => labourCount;



	/// <summary>
	/// 需求
	/// </summary>
	public DemandCollection Demands => demands;

	/// <summary>
	/// 资产
	/// </summary>
	public Asset Asset => asset;

	/// <summary>
	/// 人口的居住情况
	/// </summary>
	public PopulationResidential PopulationResidential => populationResidential;




	/// <summary>
	/// 仍未分配工作的人口数量。
	/// </summary>
	public int UnassignedLabourCount => Math.Max(0, Count - LabourCount);


	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private Population()
	{
	}


	/// <summary>
	/// 初始化人口
	/// </summary>
	public Population(string name, int count)
	{
		id = nextId++;
		this.name = name;
		this.count = count;
		this.demands = new DemandCollection(true);
		this.asset = new Asset(new());
		this.populationResidential = new PopulationResidential(Id);
	}



	/// <summary>
	/// 处理人口增长
	/// </summary>
	public void Addup(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		count += amount;
	}

	/// <summary>
	/// 处理人口死亡
	/// </summary>
	public void Death(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(amount <= Count, "死亡人数不能超过总人口数");

		// 确保死亡人数不会为负数
		count = count - amount;
		// ResidentialCount = Math.Min(ResidentialCount, Count); // TODO 这里逻辑可能需要调整，先不懂
		labourCount = Math.Min(labourCount, count); // TODO 这里逻辑可能需要调整，先不懂
	}




	/// <summary>
	/// 分配劳动力人数。
	/// </summary>
	public void AddLabour(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(labourCount + amount <= count, "劳动力分配不能超过人口总数");
		labourCount += amount;
	}


	/// <summary>
	/// 分配工作人数。
	/// </summary>
	public void AddWork(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(labourCount + amount <= count, "就业分配不能超过人口总数");
		labourCount += amount;
	}

	/// <summary>
	/// 释放工作人数。
	/// </summary>
	public void RemoveWork(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(labourCount - amount >= 0, "就业人数不能为负数");
		labourCount -= amount;
	}

	/// <summary>
	/// 按模板配置执行每日需求耗损。
	/// </summary>
	public void ConsumeDemandDaily()
	{
		Demands.GetAll()
			.ToList()
			.ForEach(demand => demand.DecayNaturally(Count));
	}





	/// <summary>
	/// 获取用于 UI 展示的需求数据
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData basicInfo = new InfoData();
		basicInfo.AddText("名称", Name);
		basicInfo.AddNumber("人口总数", Count);
		basicInfo.AddNumber("已就业", LabourCount);
		basicInfo.AddNumber("未就业", UnassignedLabourCount);

		InfoData data = new InfoData();
		data.AddGroup("人口概览", basicInfo);

		InfoData assetInfo = Asset.GetInfoData();
		if (!assetInfo.IsEmpty)
		{
			data.AddGroup("资产情况", assetInfo);
		}

		InfoData demandInfo = Demands.GetInfoData(this);
		if (!demandInfo.IsEmpty)
		{
			data.AddGroup("需求情况", demandInfo);
		}

		return data;
	}
}
