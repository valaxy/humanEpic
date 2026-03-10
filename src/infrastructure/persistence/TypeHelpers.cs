using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;


public static class TypeHelpers
{
    // 基础类型集合。
    internal static bool isBasicType(Type type)
    {
        return type == typeof(string)
            || type == typeof(bool)
            || type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }


    // 判断是否为值元组类型。
    internal static bool isValueTupleType(Type type)
    {
        if (!type.IsValueType || !type.IsGenericType)
        {
            return false;
        }

        Type genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(ValueTuple<>)
            || genericType == typeof(ValueTuple<,>)
            || genericType == typeof(ValueTuple<,,>)
            || genericType == typeof(ValueTuple<,,,>)
            || genericType == typeof(ValueTuple<,,,,>)
            || genericType == typeof(ValueTuple<,,,,,>)
            || genericType == typeof(ValueTuple<,,,,,,>)
            || genericType == typeof(ValueTuple<,,,,,,,>);
    }

    internal static bool isEntityType(Type type)
    {
        return type.GetCustomAttribute<PersistEntityAttribute>() != null;
    }

    // 判断类型是否为列表或数组。
    internal static bool isListLikeType(Type type)
    {
        return getListElementTypeOrNull(type) != null;
    }



    // 获取列表元素类型。
    internal static Type getListElementType(Type type)
    {
        Type? elementType = getListElementTypeOrNull(type);
        return elementType
            ?? throw new InvalidOperationException($"类型不是列表: {type.FullName}");
    }

    private static Type? getListElementTypeOrNull(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType()
                ?? throw new InvalidOperationException($"数组元素类型不可用: {type.FullName}");
        }

        Type? listInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
            ? type
            : type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

        if (listInterface == null)
        {
            return null;
        }

        return listInterface.GetGenericArguments()[0];
    }


    // 判断类型是否为集合（HashSet/SortedSet/ISet）。
    internal static bool isSetLikeType(Type type)
    {
        return getSetElementTypeOrNull(type) != null;
    }

    // 获取集合元素类型（HashSet/SortedSet/ISet）。
    internal static Type getSetElementType(Type type)
    {
        Type? elementType = getSetElementTypeOrNull(type);
        return elementType
            ?? throw new InvalidOperationException($"类型不是集合: {type.FullName}");
    }

    private static Type? getSetElementTypeOrNull(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
        {
            return type.GetGenericArguments()[0];
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SortedSet<>))
        {
            return type.GetGenericArguments()[0];
        }

        Type? setInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISet<>));

        if (setInterface == null)
        {
            return null;
        }

        return setInterface.GetGenericArguments()[0];
    }

    // 判断类型是否为字典。
    internal static bool isDictionaryLikeType(Type type)
    {
        return getDictionaryTypesOrNull(type) != null;
    }

    // 获取字典键值类型。
    internal static (Type keyType, Type valueType) getDictionaryTypes(Type type)
    {
        (Type keyType, Type valueType)? dictionaryTypes = getDictionaryTypesOrNull(type);
        return dictionaryTypes
            ?? throw new InvalidOperationException($"类型不是字典: {type.FullName}");
    }

    private static (Type keyType, Type valueType)? getDictionaryTypesOrNull(Type type)
    {
        Type? dictInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)
            ? type
            : type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictInterface == null)
        {
            return null;
        }

        Type[] args = dictInterface.GetGenericArguments();
        return (args[0], args[1]);
    }


    // 字典键支持基础类型和枚举。
    internal static bool isSupportedDictionaryKeyType(Type type)
    {
        return isBasicType(type) || type.IsEnum || type == typeof(Vector2I);
    }

}