using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

/// <summary>
/// 持久化反射工具：集中处理类型标记判断、成员枚举与类型解析。
/// </summary>
internal static class PersistenceReflectionHelper
{
    // 当前程序集中全部 [Persistable] 类型缓存。
    private static readonly IReadOnlyList<Type> persistableTypes = buildPersistableTypeCache();
    // 简单类名到 Persistable 类型的映射缓存。
    private static readonly IReadOnlyDictionary<string, Type> persistableTypeBySimpleName = buildPersistableTypeNameMap(persistableTypes);

    internal static bool isPersistableType(Type type)
    {
        return type.GetCustomAttribute<PersistableAttribute>() != null;
    }

    // 校验类型是否有可持久化标记。
    internal static void assertPersistableType(Type type)
    {
        Debug.Assert(isPersistableType(type), $"类型未标记 [Persistable]: {type.FullName}");
    }

    internal static bool isEntityType(Type type)
    {
        return type.GetCustomAttribute<PersistEntityAttribute>() != null;
    }

    internal static Type getEntityCollectionType(Type entityType)
    {
        PersistEntityAttribute? attr = entityType.GetCustomAttribute<PersistEntityAttribute>();
        Debug.Assert(attr != null, $"类型未标记 [PersistEntity]: {entityType.FullName}");
        return attr!.CollectionType;
    }

    internal static IReadOnlyList<Type> getAllPersistableTypesInCurrentAssembly()
    {
        return persistableTypes;
    }

    internal static Type resolvePersistableTypeBySimpleName(string typeName)
    {
        Debug.Assert(persistableTypeBySimpleName.ContainsKey(typeName), $"无法解析静态成员所属类型: {typeName}");
        return persistableTypeBySimpleName[typeName];
    }

    internal static List<(FieldInfo field, PersistFieldAttribute attr)> getPersistFields(Type type)
    {
        return enumerateTypeChain(type)
            .SelectMany(currentType => currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Select(field => (field, attr: field.GetCustomAttribute<PersistFieldAttribute>(true)))
            .Where(item => item.attr != null)
            .Select(item => (item.field, item.attr!))
            .ToList();
    }

    internal static List<(FieldInfo field, PersistFieldAttribute attr)> getPersistStaticFields(Type type)
    {
        return enumerateTypeChain(type)
            .SelectMany(currentType => currentType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Select(field => (field, attr: field.GetCustomAttribute<PersistFieldAttribute>(true)))
            .Where(item => item.attr != null)
            .Select(item => (item.field, item.attr!))
            .ToList();
    }

    internal static List<(PropertyInfo property, PersistPropertyAttribute attr)> getPersistProperties(Type type)
    {
        return enumerateTypeChain(type)
            .SelectMany(currentType => currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Select(property => (property, attr: property.GetCustomAttribute<PersistPropertyAttribute>(true)))
            .Where(item => item.attr != null)
            .Select(item => (item.property, item.attr!))
            .ToList();
    }

    internal static List<(PropertyInfo property, PersistPropertyAttribute attr)> getPersistStaticProperties(Type type)
    {
        return enumerateTypeChain(type)
            .SelectMany(currentType => currentType.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Select(property => (property, attr: property.GetCustomAttribute<PersistPropertyAttribute>(true)))
            .Where(item => item.attr != null)
            .Select(item => (item.property, item.attr!))
            .ToList();
    }

    private static IReadOnlyList<Type> buildPersistableTypeCache()
    {
        Assembly assembly = typeof(PersistableAttribute).Assembly;
        return assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(isPersistableType)
            .ToList();
    }

    private static IReadOnlyDictionary<string, Type> buildPersistableTypeNameMap(IReadOnlyList<Type> types)
    {
        List<IGrouping<string, Type>> duplicateGroups = types
            .GroupBy(type => type.Name)
            .Where(group => group.Count() > 1)
            .ToList();

        Debug.Assert(duplicateGroups.Count == 0,
            $"静态成员类型名不唯一，请避免重名: {string.Join(", ", duplicateGroups.Select(group => group.Key))}");

        return types.ToDictionary(type => type.Name, type => type);
    }

    private static IEnumerable<Type> enumerateTypeChain(Type type)
    {
        Type? current = type;
        while (current != null && current != typeof(object))
        {
            yield return current;
            current = current.BaseType;
        }
    }
}
