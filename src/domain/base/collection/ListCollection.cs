using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 基于列表的通用领域模型集合基类
/// 提供集合管理的基础功能，如添加、删除和信号通知
/// </summary>
public abstract class ListCollection<T> : ICollection<T>
{
	private readonly List<T> items = new();

	public event Action<T>? Added;
	public event Action<T>? Removed; // TODO 少了Clear时的报告？


	/// <summary>
	/// 获取集合大小
	/// </summary>
	public int Size => items.Count;

	/// <summary>
	/// 获取集合中所有的对象
	/// </summary>
	public IReadOnlyList<T> GetAll()
	{
		return items;
	}

	/// <summary>
	/// 是否包含某个对象
	/// </summary>
	public bool Has(T item)
	{
		return items.Contains(item);
	}



	/// <summary>
	/// 添加对象到集合
	/// </summary>
	public void Add(T item)
	{
		Debug.Assert(!items.Contains(item), "对象已存在于集合中");
		items.Add(item);
		Added?.Invoke(item);
	}

	/// <summary>
	/// 从集合中移除对象
	/// </summary>
	public void Remove(T item)
	{
		Debug.Assert(items.Contains(item), "对象已存在于集合中");
		items.Remove(item);
		Removed?.Invoke(item);
	}


	/// <summary>
	/// 清空集合
	/// </summary>
	public void Clear()
	{
		items.Clear();
	}
}
