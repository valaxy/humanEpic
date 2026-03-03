using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 需求集合类，管理多种需求类型及其满足情况
/// </summary>
public class DemandCollection : DictCollection<DemandType.Enums, Demand>, ICollectionPersistence, IInfo
{
	protected override DemandType.Enums GetKey(Demand item)
	{
		return item.Type;
	}

	/// <summary>
	/// 构造函数，初始化所有已知需求类型，满足度默认为 0
	/// </summary>
	public DemandCollection()
	{
		Enum
		.GetValues<DemandType.Enums>()
		.Select(demandType => new Demand(demandType, 0))
		.ToList()
		.ForEach(Add);
	}


	/// <summary>
	/// 获取用于 UI 展示的需求满足度数据
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData data = new();

		GetAll()
			.Select(demand => (label: $"{demand.Name}总满足度", degree: demand.SatisfiedAmount))
			.ToList()
			.ForEach(item => data.AddProgress(item.label, item.degree));

		return data;
	}

	public List<Dictionary<string, object>> GetSaveData()
	{
		return Enum
			.GetValues<DemandType.Enums>()
			.Select(type => Get(type))
			.Select(demand => new Dictionary<string, object>
			{
				{ "Type", (int)demand.Type },
				{ "CapitaSatisfiedAmount", demand.SatisfiedAmount }
			})
			.ToList();
	}

	public void LoadSaveData(List<Dictionary<string, object>> data)
	{
		Clear();

		data
		.ForEach(dict =>
		{
			var type = (DemandType.Enums)Convert.ToInt32(dict["Type"]);
			var capitaSatisfiedAmount = Convert.ToSingle(dict["CapitaSatisfiedAmount"]);
			Add(new Demand(type, capitaSatisfiedAmount));
		});
	}
}

