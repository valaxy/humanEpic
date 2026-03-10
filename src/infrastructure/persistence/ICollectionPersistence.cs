using System.Collections.Generic;


/// <summary>
/// 实现该接口的类可以提供可序列化的数据结构用于保存和加载
/// 仅限集合类实现
/// </summary>
public interface ICollectionPersistence
{
	/// <summary>
	/// 返回一个包含可序列化数据的数组
	/// </summary>
	public abstract List<Dictionary<string, object>> GetSaveData();

	/// <summary>
	/// 从保存数据中构造原始对象
	// TODO 不要通过list来保存？？，有的不是dict collection？
	/// </summary>
	public abstract void LoadSaveData(List<Dictionary<string, object>> data);
}
