using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 人口类，代表了具有统一特征的一群人
/// </summary>
public class Population : IIdModel, IInfo, IPersistence<Population>
{
	private static IdAllocator idAllocator = new IdAllocator();

	/// <summary>
	/// 人口模型唯一标识（自增主键）
	/// </summary>
	public int Id { get; }

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
	public int WorkCount { get; private set; } = 0;

	/// <summary>
	/// 仍未分配居住的人口数量。
	/// </summary>
	public int UnassignedResidentialCount => Math.Max(0, Count - ResidentialCount);

	/// <summary>
	/// 仍未分配工作的人口数量。
	/// </summary>
	public int UnassignedWorkCount => Math.Max(0, Count - WorkCount);


	/// <summary>
	/// 需求
	/// </summary>
	public DemandCollection Demands { get; } = new DemandCollection();



	/// <summary>
	/// 初始化人口
	/// </summary>
	public Population(int count, int? id = null)
	{
		Id = idAllocator.AllocateId(id);
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
		WorkCount = Math.Min(WorkCount, Count); // TODO 这里逻辑可能需要调整，先不懂
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
		Debug.Assert(WorkCount + amount <= Count, "就业分配不能超过人口总数");
		WorkCount += amount;
	}

	/// <summary>
	/// 释放工作人数。
	/// </summary>
	public void RemoveWork(int amount)
	{
		Debug.Assert(amount >= 0, "不能为负数");
		Debug.Assert(WorkCount - amount >= 0, "就业人数不能为负数");
		WorkCount -= amount;
	}





	/// <summary>
	/// 获取用于 UI 展示的需求数据
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData basicInfo = new InfoData();
		basicInfo.AddNumber("人口总数", Count);
		basicInfo.AddNumber("已居住", ResidentialCount);
		basicInfo.AddNumber("未居住", UnassignedResidentialCount);
		basicInfo.AddNumber("已就业", WorkCount);
		basicInfo.AddNumber("未就业", UnassignedWorkCount);

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
			{ "count", Count },
			{ "residential_count", ResidentialCount },
			{ "work_count", WorkCount },
			{ "demands", Demands.GetSaveData() },
		};
	}


	/// <summary>
	/// 从保存数据恢复人口对象
	/// </summary>
	public static Population LoadSaveData(Dictionary<string, object> data)
	{
		int id = Convert.ToInt32(data["id"]);
		int count = Convert.ToInt32(data["count"]);
		int residentialCount = Convert.ToInt32(data["residential_count"]);
		int workCount = Convert.ToInt32(data["work_count"]);

		Population population = new Population(count, id);
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
