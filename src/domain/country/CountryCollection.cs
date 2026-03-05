
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 国家集合，负责管理游戏中的国家实例。
/// </summary>
public class CountryCollection : DictCollection<int, Country>, ICollectionPersistence
{
	protected override int GetKey(Country item) => item.Id;

	public List<Dictionary<string, object>> GetSaveData()
	{
		return GetAll()
			.Select(country => new Dictionary<string, object>
			{
				{ "id", country.Id },
				{ "name", country.Name },
				{ "color", country.Color.ToHtml() }
			})
			.ToList();
	}

	public void LoadSaveData(List<Dictionary<string, object>> data)
	{
		Clear();

		data
			.Select(item => new Country(
				(string)item["name"],
				Color.FromHtml((string)item["color"]),
				System.Convert.ToInt32(item["id"])))
			.ToList()
			.ForEach(Add);
	}
}
