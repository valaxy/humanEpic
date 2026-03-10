using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 需求集合类，管理多种需求类型及其满足情况
/// </summary>
[Persistable]
public sealed class DemandCollection : DictCollection<DemandType.Enums, Demand>, IInfo<Population>
{
	protected override DemandType.Enums GetKey(Demand item) => item.TypeId;

	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private DemandCollection()
	{
	}


	/// <summary>
	/// 构造函数，初始化所有已知需求类型，满足度默认为 0
	/// </summary>
	public DemandCollection(bool autoFill)
	{
		Debug.Assert(autoFill, "这个构造函数只能用于自动填充需求类型");

		Enum
		.GetValues<DemandType.Enums>()
		.Select(demandType => new Demand(demandType, 0))
		.ToList()
		.ForEach(Add);
	}


	/// <summary>
	/// 获取用于 UI 展示的需求满足度数据
	/// </summary>
	public InfoData GetInfoData(Population population)
	{
		InfoData data = new();

		GetAll()
			.ToList()
			.ForEach(demand => data.AddProgress($"{demand.Name}总满足度", demand.SatisfiedAmount / population.Count, "", true));

		return data;
	}
}

