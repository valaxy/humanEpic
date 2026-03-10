using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 需求集合类，管理多种需求类型及其满足情况
/// </summary>
[Persistable]
public sealed class DemandCollection : DictCollection<DemandType.Enums, Demand>, IInfo
{
	protected override DemandType.Enums GetKey(Demand item) => item.Type;

	// 持久化桥接属性：通过通用持久化层序列化/反序列化集合内容。
	[PersistProperty("items")]
	public List<Demand> PersistItems
	{
		get => GetAll().ToList();
		set
		{
			Clear();
			value.ForEach(Add);
		}
	}

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
	public InfoData GetInfoData()
	{
		InfoData data = new();

		GetAll()
			.Select(demand => (label: $"{demand.Name}总满足度", degree: demand.SatisfiedAmount))
			.ToList()
			.ForEach(item => data.AddProgress(item.label, item.degree));

		return data;
	}
}

