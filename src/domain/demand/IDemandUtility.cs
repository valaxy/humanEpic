/// <summary>
/// 根据人均需求满足度，计算总效用或总效用的增速（导数）
/// - 人均需求满足度：归一化到0-1之间，0表示完全不满足，1表示完全满足
/// - 效用：一个数值，表示满足度对整体效用的贡献，可以是线性的，也可以是非线性的
/// - 效用增速（导数）：效用相对于人均需求满足度的变化率，表示满足度增加时效用的增长速度
/// </summary>
public interface IDemandUtility
{
	/// <summary>
	/// 计算总效用
	/// </summary>
	float GetUtility(float demandDegree);

	/// <summary>
	/// 计算总效用的导数
	/// </summary>
	float GetUtilityDerivative(float demandDegree); 
}
