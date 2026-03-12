using System;

/// <summary>
/// 游戏内的时间系统
/// </summary>
[Persistable]
public class TimeSystem
{
	[PersistField]
	private float elapsedDays = default!;

	/// <summary>
	/// 游戏内一天的秒数
	/// </summary>
	public const float SecondsPerDay = 5.0f;

	/// <summary>
	/// 当前游戏总时间（秒）
	/// </summary>
	public float TotalSeconds => elapsedDays * SecondsPerDay;

	/// <summary>
	/// 当前游戏总天数（可带小数）
	/// </summary>
	public float TotalDays => elapsedDays;

	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private TimeSystem()
	{
	}


	/// <summary>
	/// 构造函数，允许直接设置总天数（主要用于从存档加载时使用）。
	/// </summary>
	public TimeSystem(float elapsedDays)
	{
		this.elapsedDays = elapsedDays;
	}


	/// <summary>
	/// 获取当前是第几天（从0开始）
	/// </summary>
	/// <returns>天数</returns>
	public int GetDay()
	{
		return (int)elapsedDays;
	}

	/// <summary>
	/// 获取当前的小时（0-23）
	/// </summary>
	/// <returns>小时</returns>
	public int GetHour()
	{
		float dayFraction = elapsedDays - MathF.Floor(elapsedDays);
		return (int)(dayFraction * 24.0f);
	}




	/// <summary>
	/// 更新时间
	/// </summary>
	/// <param name="delta">与上一帧的时间差</param>
	public void Update(double delta)
	{
		elapsedDays += (float)delta / SecondsPerDay;
	}
}
