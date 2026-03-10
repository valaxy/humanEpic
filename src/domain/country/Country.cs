using Godot;


/// <summary>
/// 国家实体，表示单位和建筑的所属国家
/// </summary>
[Persistable]
public class Country : IIdModel
{
	private static readonly IdAllocator idAllocator = new();
	private int id;

	[PersistField]
	private string name = default!;

	[PersistField]
	private Color color = default!;

	/// <summary>
	/// 国家唯一标识
	/// </summary>
	[PersistProperty]
	public int Id
	{
		get => id;
		private set => id = idAllocator.AllocateId(value);
	}

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
		id = idAllocator.AllocateId();
	}
}
