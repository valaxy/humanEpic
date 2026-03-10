/// <summary>
/// 提供可视化信息数据的统一接口
/// </summary>
public interface IInfo
{
	/// <summary>
	/// 获取用于视觉层渲染的信息数据
	/// </summary>
	InfoData GetInfoData();
}


/// <summary>
/// 提供可视化信息数据的统一接口，带上下文参数版本
/// </summary>
public interface IInfo<TContext>
{
	/// <summary>
	/// 获取用于视觉层渲染的信息数据，带上下文参数版本
	/// </summary>
	InfoData GetInfoData(TContext context);
}