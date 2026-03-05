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
}
