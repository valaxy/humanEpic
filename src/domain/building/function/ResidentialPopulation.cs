using System.Diagnostics;

/// <summary>
/// 住房居住的其中一种人口
/// </summary>
public class ResidentialPopulation
{
	/// <summary>
	/// 对应人口
	/// </summary>
	public Population Pop { get; }

	/// <summary>
	/// 人数
	/// </summary>
	public int PopCount { get; private set; }

	/// <summary>
	/// 初始化
	/// </summary>
	public ResidentialPopulation(Population pop, int popCount)
	{
		Pop = pop;
		PopCount = 0;
		Increase(popCount);
	}


	/// <summary>
	/// 增加的人数不能超过总人口和剩余居住人口
	/// </summary>
	public void Increase(int count)
	{
		Debug.Assert(count >= 0, "增加人数不能为负数");
		Debug.Assert(Pop.UnassignedResidentialCount >= count, "增加人数不能超过可分配居住人口");

		PopCount += count;
		Pop.AddResidential(count);
	}

	/// <summary>
	/// 从存档数据恢复关系，不重复写入人口已居住计数。
	/// </summary>
	public static ResidentialPopulation LoadSaveData(Population pop, int popCount)
	{
		Debug.Assert(popCount >= 0, "恢复人数不能为负数");
		ResidentialPopulation relation = new ResidentialPopulation(pop, 0);
		relation.PopCount = popCount;
		return relation;
	}
}
