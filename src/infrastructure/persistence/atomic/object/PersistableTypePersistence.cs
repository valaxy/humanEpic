using System;
using System.Collections.Generic;

/// <summary>
/// 处理标记了 [Persistable] 的普通对象持久化。
/// </summary>
internal sealed class PersistableTypePersistence : ITypePersistence
{
    public bool CanHandle(Type type)
    {
        return PersistenceReflectionHelper.isPersistableType(type);
    }

    public object Serialize(object value, Type declaredType)
    {
        return DomainModelJsonPersistence.serializePersistableObject(value, declaredType);
    }

    public object Deserialize(object rawValue, Type targetType)
    {
        if (rawValue is not Dictionary<string, object> node)
        {
            throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
        }

        return DomainModelJsonPersistence.deserializePersistableObject(node, targetType);
    }
}
