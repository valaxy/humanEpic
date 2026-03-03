using Godot;


/// <summary>
/// 国家实体，表示单位和建筑的所属国家
/// </summary>
public class Country : IIdModel
{
	private static readonly IdAllocator idAllocator = new();

	/// <summary>
	/// 国家唯一标识
	/// </summary>
	public int Id { get; }

	/// <summary>
	/// 国家名称
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// 国家颜色
	/// </summary>
	public Color Color { get; }

	/// <summary>
	/// 构造国家实体
	/// </summary>
	/// <param name="name">国家名称</param>
	/// <param name="color">国家颜色</param>
	/// <param name="id">可选：指定国家唯一标识</param>
	public Country(string name, Color color, int? id = null)
	{
		Name = name;
		Color = color;
		Id = idAllocator.AllocateId(id);
	}
}
