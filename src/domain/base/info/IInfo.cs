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