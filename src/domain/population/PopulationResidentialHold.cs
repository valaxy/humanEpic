using System.Diagnostics;

/// <summary>
/// 表示人口和建筑的居住关系
/// </summary>
public class PopulationResidentialHold
{
	/// <summary>
	/// 居住建筑
	/// </summary>
	public Building Reside { get; }

	/// <summary>
	/// 人数
	/// </summary>
	public int PopCount { get; }

	/// <summary>
	/// 初始化
	/// </summary>
	public PopulationResidentialHold(Building reside, int popCount)
	{
		Debug.Assert(popCount > 0, "工作人数必须大于0");

		Reside = reside;
		PopCount = popCount;
	}
}
