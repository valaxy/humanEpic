using System.Collections.Generic;



/// <summary>
/// 接口：定义了对象如何进行双向持久化（导出和导入）。
/// </summary>
public interface IPersistence<TModel, TContext>
{
	/// <summary>
	/// 静态工厂方法：通过持久化数据字典创建一个新的对象实例。
	/// </summary>
	/// <param name="data">包含对象状态的数据字典。</param>
	/// <param name="context">上下文参数，用于传递额外的构建信息。</param>
	/// <returns>还原后的对象实例。</returns>
	static abstract TModel LoadSaveData(Dictionary<string, object> data, TContext? context = default);


	/// <summary>
	/// 获取对象的持久化数据。
	/// </summary>
	/// <returns>以字符串为键，对象状态为值的字典。这些数据应能被重建为对象的原始状态。</returns>
	Dictionary<string, object> GetSaveData();
}



/// <summary>
/// 简化版本的 IPersistence 接口，适用于不需要上下文参数的对象持久化。
/// </summary>
/// <typeparam name="TModel"></typeparam>
public interface IPersistence<TModel>
{
	/// <summary>
	/// 静态工厂方法：通过持久化数据字典创建一个新的对象实例。
	/// </summary>
	static abstract TModel LoadSaveData(Dictionary<string, object> data);

	/// <summary>
	/// 获取对象的持久化数据。
	/// </summary>
	/// <returns>以字符串为键，对象状态为值的字典。这些数据应能被重建为对象的原始状态。</returns>
	Dictionary<string, object> GetSaveData();
}