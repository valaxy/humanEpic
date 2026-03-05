using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 专门用于管理人口的集合
/// </summary>
public class PopulationCollection : DictCollection<int, Population>, ICollectionPersistence
{
	protected override int GetKey(Population item) => item.Id;

	/// <summary>
	/// 获取集合中所有人口的总数
	/// </summary>
	public int GetTotalCount()
	{
		return GetAll().Sum(population => population.Count);
	}


	public List<Dictionary<string, object>> GetSaveData()
	{
		return GetAll().Select(population => population.GetSaveData()).ToList();
	}

	public void LoadSaveData(List<Dictionary<string, object>> data)
	{
		Clear();
		data
			.Select(Population.LoadSaveData)
			.ToList()
			.ForEach(Add);
	}
}
