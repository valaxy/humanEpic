using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 人口类，代表了具有统一特征的一群人
/// - 人口是一种抽象的存在
/// - 人口具有三种功能：消费、劳动、投资
/// </summary>
public class Population : IIdModel, IInfo, IPersistence<Population>
{
	private static IdAllocator idAllocator = new IdAllocator();

	/// <summary>
	/// 人口模型唯一标识（自增主键）
	/// </summary>
	public int Id { get; }

	/// <summary>
	/// 人口名称（自然语言描述）
	/// </summary>
	public string Name { get; private set; }

	/// <summary>
	/// 人口人数
	/// </summary>
	public int Count { get; private set; }


	/// <summary>
	/// 已经定居的人数
	/// </summary>
	public int ResidentialCount { get; private set; } = 0;


	/// <summary>
	/// 已经工作的人数
	/// </summary>
	public int LabourCount { get; private set; } = 0;

	/// <summary>
	/// 仍未分配居住的人口数量。
	/// </summary>
	public int UnassignedResidentialCount => Math.Max(0, Count - ResidentialCount);

	/// <summary>
	/// 仍未分配工作的人口数量。
	/// </summary>
	public int UnassignedLabourCount => Math.Max(0, Count - LabourCount);


	/// <summary>
	/// 需求
	/// </summary>
	public DemandCollection Demands { get; } = new DemandCollection();



	/// <summary>
	/// 初始化人口
	/// </summary>
	public Population(string name, int count, int? id = null)
	{
		Id = idAllocator.AllocateId(id);
		Name = name;
		Count = count;
	}



	/// <summary>
	/// 处理人口增长
	/// </summary>
	public void Addup(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Count += amount;
	}

	/// <summary>
	/// 处理人口死亡
	/// </summary>
	public void Death(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(amount <= Count, "死亡人数不能超过总人口数");

		// 确保死亡人数不会为负数
		Count = Count - amount;
		ResidentialCount = Math.Min(ResidentialCount, Count); // TODO 这里逻辑可能需要调整，先不懂
		LabourCount = Math.Min(LabourCount, Count); // TODO 这里逻辑可能需要调整，先不懂
	}




	/// <summary>
	/// 分配居住人数。
	/// </summary>
	public void AddResidential(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(ResidentialCount + amount <= Count, "居住分配不能超过人口总数");
		ResidentialCount += amount;
	}


	/// <summary>
	/// 分配劳动力人数。
	/// </summary>
	public void AddLabour(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(LabourCount + amount <= Count, "劳动力分配不能超过人口总数");
		LabourCount += amount;
	}

	/// <summary>
	/// 释放居住人数。
	/// </summary>
	public void RemoveResidential(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(ResidentialCount - amount >= 0, "居住人数不能为负数");
		ResidentialCount -= amount;
	}



	/// <summary>
	/// 分配工作人数。
	/// </summary>
	public void AddWork(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(LabourCount + amount <= Count, "就业分配不能超过人口总数");
		LabourCount += amount;
	}

	/// <summary>
	/// 释放工作人数。
	/// </summary>
	public void RemoveWork(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(LabourCount - amount >= 0, "就业人数不能为负数");
		LabourCount -= amount;
	}





	/// <summary>
	/// 获取用于 UI 展示的需求数据
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData basicInfo = new InfoData();
		basicInfo.AddText("名称", Name);
		basicInfo.AddNumber("人口总数", Count);
		basicInfo.AddNumber("已居住", ResidentialCount);
		basicInfo.AddNumber("未居住", UnassignedResidentialCount);
		basicInfo.AddNumber("已就业", LabourCount);
		basicInfo.AddNumber("未就业", UnassignedLabourCount);

		InfoData data = new InfoData();
		data.AddGroup("人口概览", basicInfo);

		InfoData demandInfo = Demands.GetInfoData();
		if (!demandInfo.IsEmpty)
		{
			data.AddGroup("需求情况", demandInfo);
		}

		return data;
	}


	/// <summary>
	/// 获取保存数据字典
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		return new Dictionary<string, object>
		{
			{ "id", Id },
			{ "name", Name },
			{ "count", Count },
			{ "residential_count", ResidentialCount },
			{ "work_count", LabourCount },
			{ "demands", Demands.GetSaveData() },
		};
	}


	/// <summary>
	/// 从保存数据恢复人口对象
	/// </summary>
	public static Population LoadSaveData(Dictionary<string, object> data)
	{
		int id = Convert.ToInt32(data["id"]);
		string name = data.ContainsKey("name")
			? data["name"].ToString() ?? $"人口#{id}"
			: $"人口#{id}";
		int count = Convert.ToInt32(data["count"]);
		int residentialCount = Convert.ToInt32(data["residential_count"]);
		int workCount = Convert.ToInt32(data["work_count"]);

		Population population = new Population(name, count, id);
		population.AddResidential(residentialCount);
		population.AddWork(workCount);

		// TODO 这里可以优化一下？
		List<Dictionary<string, object>> demandSaveData = ((List<object>)data["demands"])
			.Select(item => (Dictionary<string, object>)item)
			.ToList();
		population.Demands.LoadSaveData(demandSaveData);
		return population;
	}
}
