using System.Collections.Generic;

/// <summary>
/// 集合接口，定义通用的集合操作
/// </summary>
public interface ICollection<T>
{
	/// <summary>
	/// 获取集合大小
	/// </summary>
	int Size { get; }

	/// <summary>
	/// 获取集合中所有的对象
	/// </summary>
	IReadOnlyList<T> GetAll();

	/// <summary>
	/// 是否包含某个对象
	/// </summary>
	bool Has(T item);




	/// <summary>
	/// 添加对象到集合
	/// </summary>
	void Add(T item);

	/// <summary>
	/// 从集合中移除对象
	/// </summary>
	void Remove(T item);

	/// <summary>
	/// 清空集合
	/// </summary>
	void Clear();
}
