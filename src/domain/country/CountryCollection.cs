
/// <summary>
/// 国家集合，负责管理游戏中的国家实例。
/// </summary>
[Persistable]
public class CountryCollection : DictCollection<int, Country>
{
	protected override int GetKey(Country item) => item.Id;
}
