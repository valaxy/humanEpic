using System.Diagnostics;

/// <summary>
/// 表示人口和建筑的关系，ValueObject
/// </summary>
public class PopBuildRelation
{
	/// <summary>
	/// 对应人口
	/// </summary>
	public Population Pop { get; }

	/// <summary>
	/// 人数
	/// </summary>
	public int PopCount { get; }

	/// <summary>
	/// 工作建筑
	/// </summary>
	public Building WorkAt { get; }

	/// <summary>
	/// 工作时的职业
	/// </summary>
	public JobType.Enums JobType { get; }

	/// <summary>
	/// 居住建筑
	/// </summary>
	public Building Reside { get; }




	/// <summary>
	/// 初始化工作地点分配
	/// </summary>
	public PopBuildRelation(Population pop, int popCount, Building workAt, JobType.Enums jobType, Building reside)
	{
		Debug.Assert(popCount > 0, "工作人数必须大于0");

		Pop = pop;
		PopCount = popCount;
		WorkAt = workAt;
		JobType = jobType;
		Reside = reside;
	}

	/// <summary>
	/// 返回一个新的Workplace实例，只有Count不同
	/// </summary>
	public PopBuildRelation SetCountReturning(int count)
	{
		Debug.Assert(count > 0, "工作人数必须大于0");
		return new PopBuildRelation(Pop, count, WorkAt, JobType, Reside);
	}
}
