using Godot;

/// <summary>
/// 饱和生存型效用：快速增长并在上限附近饱和
/// 计算公式：y(x) = 1 - e^(-kx)，满足 y(a) = b
/// 其中 x 为需求度 (0~1)，a 为目标需求度，b 为目标效用比例 (0~1)
/// TODO 还需要检查一下这个文件的逻辑
/// </summary>
public sealed class SaturationSurvivalUtility : IDemandUtility
{
	private readonly float k;
	private readonly float maxUtility; // 相当于放大效用值

	/// <summary>
	/// 初始化饱和生存型效用函数
	/// </summary>
	/// <param name="targetDegree">目标需求度 (a)</param>
	/// <param name="targetUtilityRatio">目标效用比例 (b)，通常在 0 到 1 之间</param>
	public SaturationSurvivalUtility(float targetDegree = 1.0f, float targetUtilityRatio = 0.95f, float maxUtility = 1.0f)
	{
		float a = Mathf.Max(0.001f, targetDegree);
		float b = Mathf.Clamp(targetUtilityRatio, 0.001f, 0.999f);
		k = -Mathf.Log(1.0f - b) / a;
		this.maxUtility = Mathf.Max(0.0f, maxUtility);
	}

	/// <summary>
	/// 根据需求度计算总效用
	/// </summary>
	public float GetTotalUtility(float demandDegree)
	{
		float x = Mathf.Max(0.0f, demandDegree);
		// y(x) = 1 - e^(-kx)
		float utilityRatio = 1.0f - Mathf.Exp(-k * x);
		return utilityRatio * maxUtility;
	}
}
