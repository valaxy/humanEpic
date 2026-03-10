using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	// 原子类型持久化处理器集合。
	private static readonly List<ITypePersistence> atomicTypePersistences = createAtomicTypePersistences();

	private static ITypePersistence? getAtomicTypePersistenceOrNull(Type type)
	{
		return atomicTypePersistences.FirstOrDefault(handler => handler.CanHandle(type));
	}

	private static List<ITypePersistence> createAtomicTypePersistences()
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
			new Vector2ITypePersistence()
		};
	}
}
