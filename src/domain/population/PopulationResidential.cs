using System.Collections.Generic;
using System.Diagnostics;


/// <summary>
/// 表示人口的居住情况
/// </summary>
[Persistable]
public class PopulationResidential
{
	// 人口居住在哪些建筑
	[PersistField]
	private Dictionary<int, (int ResideId, int PopCount)> holds = new Dictionary<int, (int ResideId, int PopCount)>();

	// 缓存总人口数
	[PersistField]
	private int totalPopCount = 0;

	// 反向持有引用
	[PersistField]
	private int populationId;

	/// <summary>
	/// 总人数
	/// </summary>
	public int TotalPopCount => totalPopCount;


	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private PopulationResidential()
	{
	}


	/// <summary>
	/// 构造函数
	/// </summary>
	public PopulationResidential(int populationId)
	{
		this.populationId = populationId;
	}


	/// <summary>
	/// 目前只有出生才会凭空产生新人口
	/// </summary>
	public void Birth(Building reside, int count)
	{
		Debug.Assert(count > 0, "迁入人数必须大于0");

		// 同步建筑物信息
		reside.Residential!.Add(populationId, count);

		// 同步人口居住信息
		if (holds.TryGetValue(reside.Id, out var hold))
		{
			holds[reside.Id] = (reside.Id, hold.PopCount + count);
		}
		else
		{
			holds[reside.Id] = (reside.Id, count);
		}
		totalPopCount += count;
	}


	/// <summary>
	/// 死亡会导致人口消失
	/// </summary>
	public void Death(Building reside, int count)
	{
		Debug.Assert(count > 0, "死亡人数必须大于0");
		Debug.Assert(holds.ContainsKey(reside.Id), "死亡时必须存在对应居住记录");

		(int ResideId, int PopCount) hold = holds[reside.Id];
		Debug.Assert(hold.PopCount >= count, "死亡人数不能超过该建筑内登记人数");

		// 同步建筑物信息
		int remain = hold.PopCount - count;
		bool isEmpty = remain == 0;
		reside.Residential!.Remove(populationId, count, isEmpty);

		// 同步人口居住信息
		if (isEmpty)
		{
			holds.Remove(reside.Id);
		}
		else
		{
			holds[reside.Id] = (reside.Id, remain);
		}

		totalPopCount -= count;
	}
}
