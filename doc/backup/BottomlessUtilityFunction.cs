// using Godot;

// /// <summary>
// /// 无底洞型效用：效用持续增长但边际效用递减
// /// </summary>
// public sealed class BottomlessUtilityFunction : IDemandUtilityFunction
// {
// 	private readonly float baseScale;
// 	private readonly float growthFactor;

// 	/// <summary>
// 	/// 初始化无底洞型效用函数
// 	/// </summary>
// 	public BottomlessUtilityFunction(float baseScale = 120.0f, float growthFactor = 8.0f)
// 	{
// 		this.baseScale = Mathf.Max(0.0f, baseScale);
// 		this.growthFactor = Mathf.Max(0.001f, growthFactor);
// 	}

// 	/// <summary>
// 	/// 根据需求度计算总效用
// 	/// </summary>
// 	public float CalculateTotalUtility(float demandDegree)
// 	{
// 		float clampedDegree = Mathf.Clamp(demandDegree, 0.0f, 1.0f);
// 		float scaledInput = 1.0f + growthFactor * clampedDegree;
// 		return baseScale * Mathf.Log(scaledInput);
// 	}
// }
