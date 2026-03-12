using Godot;
using System.Collections.Generic;


/// <summary>
/// 统一建筑集合，负责管理世界中所有建筑对象。
/// </summary>
[Persistable]
public class BuildingCollection : DictCollection<int, Building>
{
	// [PersistField] TODO 为什么没有持久化？
	private Dictionary<Vector2I, Building> posToItem = new();

	/// <summary>
	/// 获取建筑键值（地格坐标）。
	/// </summary>
	protected override int GetKey(Building item) => item.Id;

	public override void Add(Building item)
	{
		base.Add(item);
		posToItem[item.Collision.Center] = item;
	}

	public override void Remove(Building item)
	{
		base.Remove(item);
		posToItem.Remove(item.Collision.Center);
	}

	public override void Clear()
	{
		base.Clear();
		posToItem.Clear();
	}

	/// <summary>
	/// 根据地格坐标获取建筑对象。
	/// </summary>
	public Building GetByPos(Vector2I pos)
	{
		return posToItem[pos];
	}

	/// <summary>
	/// 检查是否存在指定地格坐标的建筑对象。
	/// </summary>
	public bool HasKeyByPos(Vector2I pos)
	{
		return posToItem.ContainsKey(pos);
	}
}
