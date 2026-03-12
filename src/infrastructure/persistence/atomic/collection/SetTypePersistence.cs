using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

internal sealed class SetTypePersistence : ITypePersistence
{
    public bool CanHandle(Type type) => TypeHelpers.isSetLikeType(type);

    public object Serialize(object value, Type declaredType)
    {
        Type elementType = TypeHelpers.getSetElementType(declaredType);

        if (value is not IEnumerable enumerable)
        {
            throw new InvalidOperationException($"类型 {declaredType.FullName} 不是可枚举集合");
        }

        List<object> items = enumerable
            .Cast<object>()
            .Select(item => DomainModelJsonPersistence.serializeValue(item, elementType))
            .ToList();

        return new Dictionary<string, object>
        {
            { "__set", true },
            { "items", items }
        };
    }

    public object Deserialize(object rawValue, Type targetType)
    {
        Type elementType = TypeHelpers.getSetElementType(targetType);

        if (rawValue is not Dictionary<string, object> setNode)
        {
            throw new InvalidOperationException($"类型 {targetType.FullName} 的数据不是对象");
        }

        if (!setNode.ContainsKey("__set") || !setNode.ContainsKey("items"))
        {
            throw new InvalidOperationException($"集合数据结构非法: {targetType.FullName}");
        }

        if (setNode["items"] is not IList rawSetItems)
        {
            throw new InvalidOperationException($"集合项结构非法: {targetType.FullName}");
        }

        object set = createSet(targetType, elementType);
        Type setType = typeof(ISet<>).MakeGenericType(elementType);
        MethodInfo addMethod = setType.GetMethod("Add")
            ?? throw new InvalidOperationException($"集合缺少 Add 方法: {targetType.FullName}");

        rawSetItems
            .Cast<object>()
            .Select(item => DomainModelJsonPersistence.deserializeValue(item, elementType))
            .ToList()
            .ForEach(item => addMethod.Invoke(set, new[] { item }));

        return set;
    }


    // 创建集合实例。
    private object createSet(Type declaredSetType, Type elementType)
    {
        if (declaredSetType.IsGenericType && declaredSetType.GetGenericTypeDefinition() == typeof(SortedSet<>))
        {
            return createSortedSetWithFallbackComparer(elementType)
                ?? throw new InvalidOperationException($"无法创建有序集合实例: {declaredSetType.FullName}");
        }

        if (declaredSetType.IsInterface || declaredSetType.IsAbstract)
        {
            return Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType))
                ?? throw new InvalidOperationException($"无法创建集合实例: {declaredSetType.FullName}");
        }

        return Activator.CreateInstance(declaredSetType)
            ?? throw new InvalidOperationException($"无法创建集合实例: {declaredSetType.FullName}");
    }

    private object createSortedSetWithFallbackComparer(Type elementType)
    {
        MethodInfo method = typeof(DomainModelJsonPersistence)
            .GetMethod(nameof(createSortedSetWithFallbackComparerGeneric), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("创建 SortedSet 失败：未找到工厂方法");
        MethodInfo genericMethod = method.MakeGenericMethod(elementType);
        return genericMethod.Invoke(null, null)
            ?? throw new InvalidOperationException($"无法创建有序集合实例: {elementType.FullName}");
    }

    private static SortedSet<T> createSortedSetWithFallbackComparerGeneric<T>()
    {
        IComparer<T> comparer = typeof(IComparable<T>).IsAssignableFrom(typeof(T)) || typeof(IComparable).IsAssignableFrom(typeof(T))
            ? Comparer<T>.Default
            : Comparer<T>.Create((left, right) =>
            {
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                if (left == null)
                {
                    return -1;
                }

                if (right == null)
                {
                    return 1;
                }

                int hashCompare = RuntimeHelpers.GetHashCode(left).CompareTo(RuntimeHelpers.GetHashCode(right));
                if (hashCompare != 0)
                {
                    return hashCompare;
                }

                return string.CompareOrdinal(left.ToString(), right.ToString());
            });

        return new SortedSet<T>(comparer);
    }

}
