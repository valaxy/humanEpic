using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 类型持久化工厂，负责根据类型提供对应的 ITypePersistence 处理器。
/// </summary>
internal static class TypePersistenceFactory
{
    // 类型持久化处理器集合（先原子/集合/实体，再一般 Persistable 对象）。
    private static readonly List<ITypePersistence> typePersistences = createTypePersistences();

    private static List<ITypePersistence> createTypePersistences()
    {
        return new List<ITypePersistence>
        {
            new StringTypePersistence(),
            new BoolTypePersistence(),
            new ByteTypePersistence(),
            new SByteTypePersistence(),
            new Int16TypePersistence(),
            new UInt16TypePersistence(),
            new Int32TypePersistence(),
            new UInt32TypePersistence(),
            new Int64TypePersistence(),
            new UInt64TypePersistence(),
            new SingleTypePersistence(),
            new DoubleTypePersistence(),
            new DecimalTypePersistence(),
            new EnumTypePersistence(),
            new ValueTupleTypePersistence(),
            new ListTypePersistence(),
            new SetTypePersistence(),
            new DictionaryTypePersistence(),
            new EntityReferenceTypePersistence(),
            new ColorTypePersistence(),
            new Vector2TypePersistence(),
            new Vector2ITypePersistence(),
            new PersistableTypePersistence() // 最后是一般的自定义对象
		};
    }

    public static ITypePersistence GetTypePersistence(Type type)
    {
        ITypePersistence? typePersistence = typePersistences.FirstOrDefault(handler => handler.CanHandle(type));
        return typePersistence ?? throw new InvalidOperationException($"类型 {type.FullName} 缺少可用的 ITypePersistence 处理器");
    }
}
