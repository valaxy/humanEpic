
using System;

/// <summary>
/// 对数衰减模型：U(x) = - k · ln(x)
/// - U(0) → +∞，极度饥饿时效用会急剧上升
/// - U(1) = 0，饱和时效用为0，过饱和时效用为负数
/// </summary>
public class LogarithmicDecayUtility : IDemandUtility
{
	// 对数衰减系数。
	private readonly float k;
	// 效用缩放上限。
	private readonly float maxUtility;

	/// <summary>
	/// 初始化对数衰减模型。
	/// </summary>
	/// <param name="decayFactor">衰减系数，越大则饥饿惩罚上升越快</param>
	/// <param name="maxUtility">最大效用缩放值</param>
	public LogarithmicDecayUtility(float decayFactor = 1.0f, float maxUtility = 1.0f)
	{
		k = MathF.Max(0.0001f, decayFactor);
		this.maxUtility = MathF.Max(0.0f, maxUtility);
	}

	/// <summary>
	/// 根据需求度计算总效用。
	/// </summary>
	public float GetUtility(float demandDegree)
	{
		float x = MathF.Max(0.0001f, demandDegree);
		return -k * MathF.Log(x) * maxUtility;
	}

}