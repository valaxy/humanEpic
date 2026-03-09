using System;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// 表示人口和建筑的居住关系
/// </summary>
public class PopulationResidential : DictCollection<int, PopulationResidentialHold>
{
	private int popCount = 0;

	/// <summary>
	/// 总人数
	/// </summary>
	public int PopCount => popCount;

	protected override int GetKey(PopulationResidentialHold value) => value.Reside.Id;

	public override void Add(PopulationResidentialHold hold)
	{
		base.Add(hold);
		popCount += hold.PopCount;
	}

	public override void Remove(PopulationResidentialHold item)
	{
		base.Remove(item);
		popCount -= item.PopCount;
	}

	public override void Clear()
	{
		base.Clear();
		popCount = 0;
	}
}
