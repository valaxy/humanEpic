/// <summary>
/// 需求效用函数接口：输入需求度（大于0），输出总效用
/// </summary>
public interface IDemandUtility
{
	/// <summary>
	/// 计算总效用
	/// </summary>
	float CalculateTotalUtility(float demandDegree);
}
