// using Godot;
// using System.Collections.Generic;

// /// <summary>
// /// 阶梯跨越型效用：只有跨过阈值才获得对应效用增量
// /// </summary>
// public sealed class StepLeapUtilityFunction : IDemandUtilityFunction
// {
// 	private readonly List<(float threshold, float utility)> steps = new List<(float threshold, float utility)>();

// 	/// <summary>
// 	/// 初始化阶梯跨越型效用函数
// 	/// </summary>
// 	public StepLeapUtilityFunction(IEnumerable<(float threshold, float utility)> configuredSteps)
// 	{
// 		foreach ((float threshold, float utility) step in configuredSteps)
// 		{
// 			float normalizedThreshold = Mathf.Clamp(step.threshold, 0.0f, 1.0f);
// 			float normalizedUtility = Mathf.Max(0.0f, step.utility);
// 			steps.Add((normalizedThreshold, normalizedUtility));
// 		}

// 		steps.Sort((left, right) => left.threshold.CompareTo(right.threshold));
// 	}

// 	/// <summary>
// 	/// 根据需求度计算总效用
// 	/// </summary>
// 	public float CalculateTotalUtility(float demandDegree)
// 	{
// 		float clampedDegree = Mathf.Clamp(demandDegree, 0.0f, 1.0f);
// 		float totalUtility = 0.0f;

// 		foreach ((float threshold, float utility) in steps)
// 		{
// 			if (clampedDegree >= threshold)
// 			{
// 				totalUtility += utility;
// 			}
// 		}

// 		return totalUtility;
// 	}
// }
