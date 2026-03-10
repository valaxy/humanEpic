using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GdUnit4;
using Godot;

[TestSuite]
public class DomainModelJsonPersistenceTest
{
	private enum SampleKind
	{
		Food = 1,
		Tool = 2
	}

	[Persistable]
	private class SampleModel
	{
		[PersistField("alias_name")]
		private string name;

		[PersistField]
		private int level;

		[PersistProperty("level_alias")]
		public int PropertyLevel { get; private set; }

		[PersistField]
		private List<int> scores;

		[PersistField]
		private Dictionary<string, int> stockByName;

		[PersistField]
		private Dictionary<SampleKind, float> demandByKind;

		public SampleModel()
		{
			name = string.Empty;
			level = 0;
			PropertyLevel = 0;
			scores = new List<int>();
			stockByName = new Dictionary<string, int>();
			demandByKind = new Dictionary<SampleKind, float>();
		}

		public SampleModel(string name, int level)
		{
			this.name = name;
			this.level = level;
			PropertyLevel = level + 1;
			scores = new List<int> { 10, 20 };
			stockByName = new Dictionary<string, int> { { "apple", 3 } };
			demandByKind = new Dictionary<SampleKind, float> { { SampleKind.Food, 12.5f } };
		}

		public string Name => name;
		public int Level => level;
		public IReadOnlyList<int> Scores => scores;
		public Dictionary<string, int> StockByName => stockByName;
		public Dictionary<SampleKind, float> DemandByKind => demandByKind;
	}

	private class NotPersistableModel
	{
		[PersistField]
		private int x;

		public NotPersistableModel()
		{
			x = 1;
		}

		public int GetValue()
		{
			return x;
		}
	}

	[Persistable]
	private class TupleModel
	{
		[PersistField]
		private (int age, float ratio) metrics;

		public TupleModel()
		{
			metrics = (0, 0.0f);
		}

		public TupleModel((int age, float ratio) metrics)
		{
			this.metrics = metrics;
		}

		public (int age, float ratio) Metrics => metrics;
	}

	[Persistable]
	private class TupleWithInvalidChildModel
	{
		#pragma warning disable CS0414
		[PersistField]
		private (NotPersistableModel invalid, int value) tupleValue;
		#pragma warning restore CS0414

		public TupleWithInvalidChildModel()
		{
			tupleValue = (new NotPersistableModel(), 0);
		}

		public TupleWithInvalidChildModel(int value)
		{
			tupleValue = (new NotPersistableModel(), value);
		}
	}

	[Persistable]
	private class GodotValueAndSetModel
	{
		[PersistField]
		private Color color;

		[PersistField]
		private Vector2 position;

		[PersistField]
		private Vector2I cell;

		[PersistField]
		private HashSet<int> tags;

		public GodotValueAndSetModel()
		{
			color = new Color(0, 0, 0, 1);
			position = Vector2.Zero;
			cell = Vector2I.Zero;
			tags = new HashSet<int>();
		}

		public GodotValueAndSetModel(Color color, Vector2 position, Vector2I cell, HashSet<int> tags)
		{
			this.color = color;
			this.position = position;
			this.cell = cell;
			this.tags = tags;
		}

		public Color Color => color;
		public Vector2 Position => position;
		public Vector2I Cell => cell;
		public HashSet<int> Tags => tags;
	}

	[Persistable]
	private class SortedSetModel
	{
		[PersistField]
		private SortedSet<int> scores;

		public SortedSetModel()
		{
			scores = new SortedSet<int>();
		}

		public SortedSetModel(IEnumerable<int> scores)
		{
			this.scores = new SortedSet<int>(scores);
		}

		public IReadOnlyList<int> Scores => scores.ToList();
	}

	[Persistable]
	private class StaticFieldModel
	{
		[PersistField]
		private static int staticCounter;

		[PersistField]
		private int value;

		public StaticFieldModel()
		{
			value = 0;
		}

		public StaticFieldModel(int value)
		{
			this.value = value;
		}

		public int Value => value;

		public static int StaticCounter
		{
			get => staticCounter;
			set => staticCounter = value;
		}
	}

	[TestCase]
	public void Save_ShouldContainExpectedJsonShape()
	{
		SampleModel model = new SampleModel("alpha", 7);
		string json = DomainModelJsonPersistence.Save(model);
		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement root = document.RootElement;

		if (root.ValueKind != JsonValueKind.Object)
		{
			throw new Exception("JSON 根节点必须是对象");
		}

		if (!root.TryGetProperty("__static", out JsonElement staticNode) || staticNode.ValueKind != JsonValueKind.Object)
		{
			throw new Exception("根节点必须包含 __static 对象");
		}

		if (!root.TryGetProperty("alias_name", out JsonElement nameNode) || nameNode.GetString() != "alpha")
		{
			throw new Exception("字段别名 alias_name 序列化异常");
		}

		if (!root.TryGetProperty("level_alias", out JsonElement levelAliasNode) || levelAliasNode.GetInt32() != 8)
		{
			throw new Exception("属性别名 level_alias 序列化异常");
		}

		if (!root.TryGetProperty("scores", out JsonElement scoreNode) || scoreNode.ValueKind != JsonValueKind.Array || scoreNode.GetArrayLength() != 2)
		{
			throw new Exception("List 字段序列化格式异常");
		}

		if (!root.TryGetProperty("stockByName", out JsonElement stockNode) || stockNode.ValueKind != JsonValueKind.Object)
		{
			throw new Exception("Dictionary 字段必须被包装为对象");
		}

		if (!stockNode.TryGetProperty("__dict", out JsonElement dictTagNode) || !dictTagNode.GetBoolean())
		{
			throw new Exception("Dictionary 缺少 __dict 标记");
		}

		if (!stockNode.TryGetProperty("entries", out JsonElement entriesNode) || entriesNode.ValueKind != JsonValueKind.Array || entriesNode.GetArrayLength() != 1)
		{
			throw new Exception("Dictionary entries 结构异常");
		}

		JsonElement firstEntry = entriesNode[0];
		if (!firstEntry.TryGetProperty("k", out JsonElement keyNode) || keyNode.GetString() != "apple")
		{
			throw new Exception("Dictionary 条目键格式异常");
		}

		if (!firstEntry.TryGetProperty("v", out JsonElement valueNode) || valueNode.GetInt32() != 3)
		{
			throw new Exception("Dictionary 条目值格式异常");
		}
	}

	[TestCase]
	public void SaveAndLoad_ShouldRoundTrip()
	{
		SampleModel model = new SampleModel("beta", 11);
		string json = DomainModelJsonPersistence.Save(model);
		SampleModel loaded = DomainModelJsonPersistence.Load<SampleModel>(json);

		if (loaded.Name != "beta" || loaded.Level != 11)
		{
			throw new Exception("基本字段反序列化结果不正确");
		}

		if (loaded.PropertyLevel != 12)
		{
			throw new Exception("属性反序列化结果不正确");
		}

		if (loaded.Scores.Count != 2 || loaded.Scores[0] != 10 || loaded.Scores[1] != 20)
		{
			throw new Exception("List 字段反序列化结果不正确");
		}

		if (!loaded.StockByName.ContainsKey("apple") || loaded.StockByName["apple"] != 3)
		{
			throw new Exception("Dictionary 字段反序列化结果不正确");
		}

		if (!loaded.DemandByKind.ContainsKey(SampleKind.Food) || Math.Abs(loaded.DemandByKind[SampleKind.Food] - 12.5f) > 0.001f)
		{
			throw new Exception("Enum Key Dictionary 字段反序列化结果不正确");
		}
	}

	[TestCase]
	public void Save_NonPersistableModel_ShouldThrow()
	{
		NotPersistableModel model = new NotPersistableModel();
		_ = model.GetValue();
		bool hasThrown = false;

		try
		{
			DomainModelJsonPersistence.Save(model);
		}
		catch (InvalidOperationException)
		{
			hasThrown = true;
		}

		if (!hasThrown)
		{
			throw new Exception("未标记 [Persistable] 的类型应抛出异常");
		}
	}

	[TestCase]
	public void SaveAndLoad_ValueTuple_ShouldRoundTrip()
	{
		TupleModel model = new TupleModel((18, 0.75f));
		string json = DomainModelJsonPersistence.Save(model);
		TupleModel loaded = DomainModelJsonPersistence.Load<TupleModel>(json);

		if (loaded.Metrics.age != 18)
		{
			throw new Exception("值元组 age 反序列化结果不正确");
		}

		if (Math.Abs(loaded.Metrics.ratio - 0.75f) > 0.001f)
		{
			throw new Exception("值元组 ratio 反序列化结果不正确");
		}
	}

	[TestCase]
	public void Save_TupleWithInvalidChild_ShouldThrow()
	{
		TupleWithInvalidChildModel model = new TupleWithInvalidChildModel(3);
		bool hasThrown = false;

		try
		{
			DomainModelJsonPersistence.Save(model);
		}
		catch (InvalidOperationException)
		{
			hasThrown = true;
		}

		if (!hasThrown)
		{
			throw new Exception("值元组子类型不可持久化时应抛出异常");
		}
	}

	[TestCase]
	public void SaveAndLoad_GodotValueTypesAndHashSet_ShouldRoundTrip()
	{
		GodotValueAndSetModel model = new GodotValueAndSetModel(
			new Color(0.12f, 0.34f, 0.56f, 0.78f),
			new Vector2(12.5f, -3.25f),
			new Vector2I(8, 9),
			new HashSet<int> { 2, 5, 7 }
		);

		string json = DomainModelJsonPersistence.Save(model);
		GodotValueAndSetModel loaded = DomainModelJsonPersistence.Load<GodotValueAndSetModel>(json);

		if (!loaded.Color.IsEqualApprox(model.Color))
		{
			throw new Exception("Color 反序列化结果不正确");
		}

		if (!loaded.Position.IsEqualApprox(model.Position))
		{
			throw new Exception("Vector2 反序列化结果不正确");
		}

		if (loaded.Cell != model.Cell)
		{
			throw new Exception("Vector2I 反序列化结果不正确");
		}

		if (!loaded.Tags.SetEquals(model.Tags))
		{
			throw new Exception("HashSet 反序列化结果不正确");
		}
	}

	[TestCase]
	public void SaveAndLoad_SortedSet_ShouldRoundTrip()
	{
		SortedSetModel model = new SortedSetModel(new[] { 7, 3, 9, 3 });
		string json = DomainModelJsonPersistence.Save(model);
		SortedSetModel loaded = DomainModelJsonPersistence.Load<SortedSetModel>(json);

		if (loaded.Scores.Count != 3)
		{
			throw new Exception("SortedSet 去重反序列化结果不正确");
		}

		if (loaded.Scores[0] != 3 || loaded.Scores[1] != 7 || loaded.Scores[2] != 9)
		{
			throw new Exception("SortedSet 排序反序列化结果不正确");
		}
	}

	[TestCase]
	public void SaveAndLoad_StaticField_ShouldPersistAtRootSidecar()
	{
		StaticFieldModel.StaticCounter = 42;
		StaticFieldModel model = new StaticFieldModel(9);

		Dictionary<string, object> saved = DomainModelJsonPersistence.SaveToObject(model);
		if (!saved.ContainsKey("__static"))
		{
			throw new Exception("根节点缺少 __static");
		}

		StaticFieldModel.StaticCounter = 0;
		StaticFieldModel loaded = DomainModelJsonPersistence.LoadFromObject<StaticFieldModel>(saved);

		if (loaded.Value != 9)
		{
			throw new Exception("实例字段反序列化结果不正确");
		}

		if (StaticFieldModel.StaticCounter != 42)
		{
			throw new Exception("静态字段反序列化结果不正确");
		}
	}
}
