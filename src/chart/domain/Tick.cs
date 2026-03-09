using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 坐标轴刻度生成策略。
/// </summary>
public sealed class Tick
{
    // 默认刻度数量。
    private const int DefaultTickCount = 5;

    // 刻度生成函数。
    private readonly Func<float, float, IReadOnlyList<float>> generator;

    private Tick(Func<float, float, IReadOnlyList<float>> generator)
    {
        this.generator = generator;
    }

    /// <summary>
    /// 生成刻度值序列。
    /// </summary>
    public IReadOnlyList<float> Generate(float min, float max)
    {
        if (max < min)
        {
            return generator(max, min);
        }

        return generator(min, max);
    }

    /// <summary>
    /// 线性等分刻度。
    /// </summary>
    public static Tick Linear(int tickCount = DefaultTickCount)
    {
        int safeCount = Math.Max(2, tickCount);
        return new Tick((min, max) =>
        {
            if (MathF.Abs(max - min) < 0.0001f)
            {
                return [min];
            }

            return Enumerable.Range(0, safeCount)
                .Select(index => min + (max - min) * index / (safeCount - 1))
                .ToList();
        });
    }

    /// <summary>
    /// 按 1、10、100、1000... 级别生成刻度。
    /// </summary>
    public static Tick PowerOfTen()
    {
        return new Tick((min, max) =>
        {
            float bound = MathF.Max(MathF.Abs(min), MathF.Abs(max));
            if (bound < 1.0f)
            {
                bound = 1.0f;
            }

            float step = MathF.Pow(10.0f, MathF.Floor(MathF.Log10(bound)));
            float start = MathF.Floor(min / step) * step;
            float end = MathF.Ceiling(max / step) * step;
            int count = Math.Max(1, (int)MathF.Round((end - start) / step) + 1);

            return Enumerable.Range(0, count)
                .Select(index => start + index * step)
                .ToList();
        });
    }
}
