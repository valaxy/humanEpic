using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

internal sealed class ValueTupleTypePersistence : ITypePersistence
{
    public bool CanHandle(Type type) => TypeHelpers.isValueTupleType(type);

    public object Serialize(object value, Type declaredType)
    {
        Type[] tupleElementTypes = getValueTupleElementTypes(declaredType);
        List<object> tupleValues = tupleElementTypes
            .Select((elementType, index) =>
            {
                string fieldName = $"Item{index + 1}";
                object tupleElement = declaredType.GetField(fieldName)?.GetValue(value)
                    ?? throw new InvalidOperationException($"值元组字段读取失败: {declaredType.FullName}.{fieldName}");
                return DomainModelJsonPersistence.serializeValue(tupleElement, elementType);
            })
            .ToList();

        return new Dictionary<string, object>
        {
            { "__tuple", true },
            { "items", tupleValues }
        };
    }

    public object Deserialize(object rawValue, Type targetType)
    {
        if (rawValue is not Dictionary<string, object> tupleNode)
        {
            throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
        }

        if (!tupleNode.ContainsKey("__tuple") || !tupleNode.ContainsKey("items"))
        {
            throw new InvalidOperationException($"值元组数据结构非法: {targetType.FullName}");
        }

        if (tupleNode["items"] is not IList tupleItemsRaw)
        {
            throw new InvalidOperationException($"值元组项结构非法: {targetType.FullName}");
        }

        Type[] tupleElementTypes = getValueTupleElementTypes(targetType);
        if (tupleItemsRaw.Count != tupleElementTypes.Length)
        {
            throw new InvalidOperationException($"值元组元素数量不匹配: {targetType.FullName}");
        }

        object[] tupleValues = tupleElementTypes
            .Select((elementType, index) =>
            {
                object rawTupleElement = tupleItemsRaw[index]
                    ?? throw new InvalidOperationException($"值元组元素不能为空: {targetType.FullName}.Item{index + 1}");
                return DomainModelJsonPersistence.deserializeValue(rawTupleElement, elementType);
            })
            .ToArray();

        object? tupleValue = Activator.CreateInstance(targetType, tupleValues);
        return tupleValue ?? throw new InvalidOperationException($"值元组实例化失败: {targetType.FullName}");
    }


    // 获取值元组元素类型。
    private static Type[] getValueTupleElementTypes(Type tupleType)
    {
        Debug.Assert(TypeHelpers.isValueTupleType(tupleType), $"类型不是值元组: {tupleType.FullName}");
        return tupleType.GetGenericArguments();
    }
}
