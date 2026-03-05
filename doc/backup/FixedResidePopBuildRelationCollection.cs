using System;

/// <summary>
/// 固定居住人口建筑关系集合。
/// </summary>
public class FixedResidePopBuildRelationCollection : PopBuildRelationCollection
{
	// 固定建筑对象。
	private readonly Building fixedBuilding;

	/// <summary>
	/// 初始化固定建筑工作分配集合。
	/// </summary>
	public FixedResidePopBuildRelationCollection(Building fixedBuilding)
	{
		this.fixedBuilding = fixedBuilding;
	}

	/// <summary>
	/// 校验工作分配是否合法。
	/// </summary>
	protected override void validateWorkplace(PopBuildRelation workplace)
	{
		if (workplace.Reside != fixedBuilding)
		{
			throw new InvalidOperationException("固定居住人口只能分配到指定的建筑。");
		}
	}

	protected override void onAdd(PopBuildRelation item)
	{
		item.Worker.WorkAt.items.Add(item);
	}

	protected override void onRemove(PopBuildRelation item)
	{
		item.Worker.WorkAt.items.Remove(item);
	}
}
