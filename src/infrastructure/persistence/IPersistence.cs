using System.Collections.Generic;


public interface IPersistenceRead
{
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
public interface IPersistence<TModel> : IPersistenceRead
{
    /// <summary>
    /// 静态工厂方法：通过持久化数据字典创建一个新的对象实例。
    /// </summary>
    static abstract TModel LoadSaveData(Dictionary<string, object> data);
}