using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
    // 字典元数据键：标记当前对象节点是字典包装结构。
    private const string dictTag = "__dict";
    // 字典元数据键：字典键值对象键名（扁平格式）。
    private const string dictEntries = "kv";
    // 集合元数据键：标记当前对象节点是集合包装结构。
    private const string setTag = "__set";
    // 集合元数据键：集合元素列表键名。
    private const string setItems = "items";
    // 值元组元数据键：标记当前对象节点是值元组包装结构。
    private const string tupleTag = "__tuple";
    // 值元组元数据键：值序列键名。
    private const string tupleItems = "items";
    // Godot 值类型元数据键：标记当前对象节点是 Godot 值类型包装结构。
    private const string godotTag = "__godot";
    // Godot 值类型元数据键：类型名。
    private const string godotType = "type";
    // 根节点静态成员键：按类型分组保存静态字段/属性。
    private const string staticMembers = "__static";

    /// <summary>
    /// 将模型保存为中间字典对象。
    /// </summary>
    public static Dictionary<string, object> SaveToObject<TModel>(TModel model) where TModel : class
    {
        if (model == null)
        {
            throw new InvalidOperationException("持久化模型不能为空");
        }

        Type modelType = model.GetType();
        PersistenceReflectionHelper.assertPersistableType(modelType);
        return saveRootObject(model, modelType, modelType, Array.Empty<object>(), true);
    }

    /// <summary>
    /// 将模型保存为中间字典对象，不附带根节点 __static。
    /// </summary>
    public static Dictionary<string, object> SaveToObjectWithoutStatic<TModel>(TModel model) where TModel : class
    {
        if (model == null)
        {
            throw new InvalidOperationException("持久化模型不能为空");
        }

        Type modelType = model.GetType();
        PersistenceReflectionHelper.assertPersistableType(modelType);
        return saveRootObject(model, modelType, modelType, Array.Empty<object>(), false);
    }

    /// <summary>
    /// 将模型保存为中间字典对象，不附带根节点 __static，并附带实体集合上下文。
    /// </summary>
    public static Dictionary<string, object> SaveToObjectWithoutStatic<TModel>(TModel model, IEnumerable<object> entityCollections) where TModel : class
    {
        if (model == null)
        {
            throw new InvalidOperationException("持久化模型不能为空");
        }

        Type modelType = model.GetType();
        PersistenceReflectionHelper.assertPersistableType(modelType);
        return saveRootObject(model, modelType, modelType, entityCollections.ToList(), false);
    }




    /// <summary>
    /// 从中间字典对象加载模型。
    /// </summary>
    public static TModel LoadFromObject<TModel>(Dictionary<string, object> data) where TModel : class
    {
        Type modelType = typeof(TModel);
        PersistenceReflectionHelper.assertPersistableType(modelType);
        return (TModel)loadRootObject(data, modelType, modelType, Array.Empty<object>(), true);
    }

    /// <summary>
    /// 从中间字典对象加载模型，忽略根节点 __static。
    /// </summary>
    public static TModel LoadFromObjectWithoutStatic<TModel>(Dictionary<string, object> data) where TModel : class
    {
        Type modelType = typeof(TModel);
        PersistenceReflectionHelper.assertPersistableType(modelType);
        return (TModel)loadRootObject(data, modelType, modelType, Array.Empty<object>(), false);
    }

    /// <summary>
    /// 从中间字典对象加载模型，忽略根节点 __static，并附带实体集合上下文。
    /// </summary>
    public static TModel LoadFromObjectWithoutStatic<TModel>(Dictionary<string, object> data, IEnumerable<object> entityCollections) where TModel : class
    {
        Type modelType = typeof(TModel);
        PersistenceReflectionHelper.assertPersistableType(modelType);
        return (TModel)loadRootObject(data, modelType, modelType, entityCollections.ToList(), false);
    }





    /// <summary>
    /// 从根节点中提取 __static 并删除该键。
    /// </summary>
    public static Dictionary<string, object> ExtractStaticMembers(Dictionary<string, object> data)
    {
        if (!data.TryGetValue(staticMembers, out object? staticRaw))
        {
            return new Dictionary<string, object>();
        }

        if (staticRaw is not Dictionary<string, object> staticNode)
        {
            throw new InvalidOperationException("根节点 __static 结构非法");
        }

        data.Remove(staticMembers);
        return staticNode.ToDictionary(entry => entry.Key, entry => entry.Value);
    }

    /// <summary>
    /// 将 __static 写入根节点。
    /// </summary>
    public static void AttachStaticMembers(Dictionary<string, object> data, Dictionary<string, object> staticNode)
    {
        if (staticNode.Count == 0)
        {
            return;
        }

        if (data.ContainsKey(staticMembers))
        {
            throw new InvalidOperationException("根节点已存在 __static，禁止重复写入");
        }

        data[staticMembers] = staticNode;
    }

    /// <summary>
    /// 合并多个 __static 节点。
    /// </summary>
    public static Dictionary<string, object> MergeStaticMembers(IEnumerable<Dictionary<string, object>> staticNodes)
    {
        Dictionary<string, object> merged = new Dictionary<string, object>();
        staticNodes
            .Where(staticNode => staticNode != null)
            .ToList()
            .ForEach(staticNode =>
            {
                staticNode
                    .ToList()
                    .ForEach(entry =>
                    {
                        if (merged.ContainsKey(entry.Key))
                        {
                            throw new InvalidOperationException($"__static 存在重复类型键: {entry.Key}");
                        }

                        merged[entry.Key] = entry.Value;
                    });
            });

        return merged;
    }

    /// <summary>
    /// 立即应用 __static 节点到静态字段/属性。
    /// </summary>
    public static void ApplyStaticMembers(Dictionary<string, object> staticNode)
    {
        deserializeStaticMembers(staticNode);
    }
}
