using Godot;


/// <summary>
/// 国家实体，表示单位和建筑的所属国家
/// </summary>
[Persistable]
[PersistEntity(typeof(CountryCollection))]
public class Country : IIdModel
{
	[PersistField]
	private static int nextId = 1;

	[PersistField]
	private int id = default!;

	[PersistField]
	private string name = default!;

	[PersistField]
	private Color color = default!;

	/// <summary>
	/// 国家唯一标识
	/// </summary>
	public int Id => id;

	/// <summary>
	/// 国家名称
	/// </summary>
	public string Name => name;

	/// <summary>
	/// 国家颜色
	/// </summary>
	public Color Color => color;

	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private Country()
	{
	}

	/// <summary>
	/// 构造国家实体
	/// </summary>
	/// <param name="name">国家名称</param>
	/// <param name="color">国家颜色</param>
	public Country(string name, Color color)
	{
		this.name = name;
		this.color = color;
		this.id = nextId++;
	}
}
