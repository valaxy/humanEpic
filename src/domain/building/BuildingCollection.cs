using Godot;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// 统一建筑集合，负责管理世界中所有建筑对象。
/// </summary>
[Persistable]
public class BuildingCollection : DictCollection<Vector2I, Building>
{
	private readonly Dictionary<int, Building> idToItem = new();

	/// <summary>
	/// 获取建筑键值（地格坐标）。
	/// </summary>
	protected override Vector2I GetKey(Building item) => item.Collision.Center;

	public override void Add(Building item)
	{
		base.Add(item);
		idToItem[item.Id] = item;
	}

	public override void Remove(Building item)
	{
		base.Remove(item);
		idToItem.Remove(item.Id);
	}

	public override void Clear()
	{
		base.Clear();
		idToItem.Clear();
	}



	public Building GetById(int id)
	{
		if (idToItem.TryGetValue(id, out Building? building))
		{
			return building;
		}

		Building loadedBuilding = GetAll().First(item => item.Id == id);
		idToItem[id] = loadedBuilding;
		return loadedBuilding;
	}
}
