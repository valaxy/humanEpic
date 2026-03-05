using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 基于字典的通用领域模型集合基类
/// 提供基于 Key 的高效查找，同时满足 ICollection 接口
/// </summary>
public abstract class DictCollection<TKey, TValue> : ICollection<TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> items = new();

    public event Action<TValue>? Added;

    public event Action<TValue>? Removed;

    /// <summary>
    /// 获取集合大小
    /// </summary>
    public int Size => items.Count;

    /// <summary>
    /// 是否包含某个对象
    /// </summary>
    public bool Has(TValue item)
    {
        return items.ContainsKey(GetKey(item));
    }

    /// <summary>
    /// 获取集合中所有的对象
    /// </summary>
    public IReadOnlyList<TValue> GetAll()
    {
        return items.Values.ToList();
    }

    /// <summary>
    /// 是否包含某个键
    /// </summary>
    public bool HasKey(TKey key)
    {
        return items.ContainsKey(key);
    }

    /// <summary>
    /// 根据键获取对象
    /// </summary>
    public TValue Get(TKey key)
    {
        return items[key];
    }




    /// <summary>
    /// 添加对象到集合
    /// </summary>
    public virtual void Add(TValue item)
    {
        TKey key = GetKey(item);
        Debug.Assert(!items.ContainsKey(key), "对象已存在于集合中");

        items.Add(key, item);
        Added?.Invoke(item);
    }

    /// <summary>
    /// 从集合中移除对象
    /// </summary>
    public virtual void Remove(TValue item)
    {
        TKey key = GetKey(item);
        Debug.Assert(items.ContainsKey(key), "对象不存在于集合中");

        items.Remove(key);
        Removed?.Invoke(item);
    }

    /// <summary>
    /// 清空集合
    /// </summary>
    public virtual void Clear()
    {
        items.Clear();
    }



    /// <summary>
    /// 获取键的方法，子类需实现
    /// </summary>
    protected abstract TKey GetKey(TValue item);
}
