using System;
using System.Globalization;

internal sealed class EntityReferenceTypePersistence : ITypePersistence
{
	public bool CanHandle(Type type) => TypeHelpers.isEntityType(type);

	public object Serialize(object value, Type declaredType)
	{
		if (!DomainModelJsonPersistence.shouldSerializeEntityAsFullObject(declaredType))
		{
			if (value is not IIdModel idModel)
			{
				throw new InvalidOperationException($"实体类型必须实现 IIdModel: {declaredType.FullName}");
			}

			return idModel.Id;
		}

		return DomainModelJsonPersistence.serializePersistableObject(value, declaredType);
	}

	public object Deserialize(object rawValue, Type targetType)
	{
		if (!DomainModelJsonPersistence.shouldSerializeEntityAsFullObject(targetType))
		{
			int entityId = Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
			return DomainModelJsonPersistence.resolveEntityById(targetType, entityId);
		}

		if (rawValue is not System.Collections.Generic.Dictionary<string, object> node)
		{
			throw new InvalidOperationException($"实体完整节点结构非法: {targetType.FullName}");
		}

		return DomainModelJsonPersistence.deserializePersistableObject(node, targetType);
	}
}
