using System.Collections.Generic;

/// <summary>
/// 模板会在程序开始时初始化，保存公共的数据
/// </summary>
/// <typeparam name="TKey">字典键类型</typeparam>
/// <typeparam name="TTemplate">模板类型，就是类型自己</typeparam>
public interface ITemplate<TKey, TTemplate>
    where TKey : notnull
{
    /// <summary>
    /// 获取所有模板实例的字典，键为模板的唯一标识符
    /// </summary>
    public static abstract Dictionary<TKey, TTemplate> GetTemplates();

    /// <summary>
    /// 获取指定类型的模板
    /// </summary>
    public static abstract TTemplate GetTemplate(TKey key);
}