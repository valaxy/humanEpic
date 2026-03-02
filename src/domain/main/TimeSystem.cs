/// <summary>
/// 游戏内的时间系统
/// </summary>
public class TimeSystem
{
	/// <summary>
	/// 游戏内一天的秒数
	/// </summary>
	public const float SecondsPerDay = 5.0f;

	/// <summary>
	/// 当前游戏总时间（秒）
	/// </summary>
	public float TotalSeconds { get; private set; } = 0.0f;

	/// <summary>
	/// 更新时间
	/// </summary>
	/// <param name="delta">与上一帧的时间差</param>
	public void Update(double delta)
	{
		TotalSeconds += (float)delta;
	}

	/// <summary>
	/// 获取当前是第几天（从0开始）
	/// </summary>
	/// <returns>天数</returns>
	public int GetDay()
	{
		return (int)(TotalSeconds / SecondsPerDay);
	}

	/// <summary>
	/// 获取当前的小时（0-23）
	/// </summary>
	/// <returns>小时</returns>
	public int GetHour()
	{
		float secondsInDay = TotalSeconds % SecondsPerDay;
		return (int)((secondsInDay / SecondsPerDay) * 24.0f);
	}
}
