using System.Linq;

/// <summary>
/// 专门用于管理人口的集合
/// </summary>
[Persistable]
public class PopulationCollection : DictCollection<int, Population>
{
	protected override int GetKey(Population item) => item.Id;

	public PopulationCollection()
	{

	}
	
	/// <summary>
	/// 获取集合中所有人口的总数
	/// </summary>
	public int GetTotalCount()
	{
		return GetAll().Sum(population => population.Count);
	}

}
