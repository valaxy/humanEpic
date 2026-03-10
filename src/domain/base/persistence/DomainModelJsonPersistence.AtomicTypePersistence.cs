using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// 领域模型特性驱动持久化器，负责 Save/Load 到 JSON。
/// </summary>
public static partial class DomainModelJsonPersistence
{
	// 原子类型持久化器映射：一个类型一个处理模块。
	private static readonly Dictionary<Type, IAtomicTypePersistence> atomicTypePersistences =
		createAtomicTypePersistences();

	private interface IAtomicTypePersistence
	{
		Type TargetType { get; }
		object Serialize(object value);
		object Deserialize(object rawValue);
	}

	private static bool trySerializeAtomicValue(object value, Type type, out object serialized)
	{
		if (!atomicTypePersistences.TryGetValue(type, out IAtomicTypePersistence? persistence))
		{
			serialized = null!;
			return false;
		}

		serialized = persistence.Serialize(value);
		return true;
	}

	private static bool tryDeserializeAtomicValue(object rawValue, Type type, out object deserialized)
	{
		if (!atomicTypePersistences.TryGetValue(type, out IAtomicTypePersistence? persistence))
		{
			deserialized = null!;
			return false;
		}

		deserialized = persistence.Deserialize(rawValue);
		return true;
	}

	private static Dictionary<Type, IAtomicTypePersistence> createAtomicTypePersistences()
	{
		List<IAtomicTypePersistence> handlers = new List<IAtomicTypePersistence>
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
			new ColorTypePersistence(),
			new Vector2TypePersistence(),
			new Vector2ITypePersistence()
		};

		return handlers.ToDictionary(handler => handler.TargetType, handler => handler);
	}

	private sealed class StringTypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(string);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue)
		{
			return rawValue.ToString() ?? throw new InvalidOperationException("字符串转换失败");
		}
	}

	private sealed class BoolTypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(bool);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToBoolean(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class ByteTypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(byte);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToByte(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class SByteTypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(sbyte);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToSByte(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class Int16TypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(short);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToInt16(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class UInt16TypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(ushort);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToUInt16(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class Int32TypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(int);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class UInt32TypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(uint);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToUInt32(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class Int64TypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(long);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToInt64(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class UInt64TypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(ulong);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToUInt64(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class SingleTypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(float);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToSingle(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class DoubleTypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(double);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class DecimalTypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(decimal);
		public object Serialize(object value) => value;
		public object Deserialize(object rawValue) => Convert.ToDecimal(rawValue, CultureInfo.InvariantCulture);
	}

	private sealed class ColorTypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(Color);

		public object Serialize(object value)
		{
			Color color = (Color)value;
			return new Dictionary<string, object>
			{
				{ godotTag, true },
				{ godotType, nameof(Color) },
				{ godotData, new Dictionary<string, object>
					{
						{ "r", color.R },
						{ "g", color.G },
						{ "b", color.B },
						{ "a", color.A }
					}
				}
			};
		}

		public object Deserialize(object rawValue)
		{
			Dictionary<string, object> data = readGodotNode(rawValue, nameof(Color));
			float r = Convert.ToSingle(data["r"], CultureInfo.InvariantCulture);
			float g = Convert.ToSingle(data["g"], CultureInfo.InvariantCulture);
			float b = Convert.ToSingle(data["b"], CultureInfo.InvariantCulture);
			float a = Convert.ToSingle(data["a"], CultureInfo.InvariantCulture);
			return new Color(r, g, b, a);
		}
	}

	private sealed class Vector2TypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(Vector2);

		public object Serialize(object value)
		{
			Vector2 vector = (Vector2)value;
			return new Dictionary<string, object>
			{
				{ godotTag, true },
				{ godotType, nameof(Vector2) },
				{ godotData, new Dictionary<string, object>
					{
						{ "x", vector.X },
						{ "y", vector.Y }
					}
				}
			};
		}

		public object Deserialize(object rawValue)
		{
			Dictionary<string, object> data = readGodotNode(rawValue, nameof(Vector2));
			float x = Convert.ToSingle(data["x"], CultureInfo.InvariantCulture);
			float y = Convert.ToSingle(data["y"], CultureInfo.InvariantCulture);
			return new Vector2(x, y);
		}
	}

	private sealed class Vector2ITypePersistence : IAtomicTypePersistence
	{
		public Type TargetType => typeof(Vector2I);

		public object Serialize(object value)
		{
			Vector2I vector = (Vector2I)value;
			return new Dictionary<string, object>
			{
				{ godotTag, true },
				{ godotType, nameof(Vector2I) },
				{ godotData, new Dictionary<string, object>
					{
						{ "x", vector.X },
						{ "y", vector.Y }
					}
				}
			};
		}

		public object Deserialize(object rawValue)
		{
			Dictionary<string, object> data = readGodotNode(rawValue, nameof(Vector2I));
			int x = Convert.ToInt32(data["x"], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(data["y"], CultureInfo.InvariantCulture);
			return new Vector2I(x, y);
		}
	}

	private static Dictionary<string, object> readGodotNode(object rawValue, string targetTypeName)
	{
		if (rawValue is not Dictionary<string, object> node)
		{
			throw new InvalidOperationException($"Godot 值类型数据结构非法: {targetTypeName}");
		}

		if (!node.ContainsKey(godotTag) || !node.ContainsKey(godotType) || !node.ContainsKey(godotData))
		{
			throw new InvalidOperationException($"Godot 值类型缺少必要键: {targetTypeName}");
		}

		if (node[godotData] is not Dictionary<string, object> data)
		{
			throw new InvalidOperationException($"Godot 值类型 data 结构非法: {targetTypeName}");
		}

		return data;
	}
}