using System;
using System.Collections.Generic;

/// <summary>
/// 游戏内的时间系统
/// </summary>
public class TimeSystem : IPersistence<TimeSystem>
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
	/// 构造函数，允许直接设置总时间（主要用于从存档加载时使用）。
	/// </summary>
	public TimeSystem(float totalSeconds)
	{
		TotalSeconds = totalSeconds;
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




	/// <summary>
	/// 更新时间
	/// </summary>
	/// <param name="delta">与上一帧的时间差</param>
	public void Update(double delta)
	{
		TotalSeconds += (float)delta;
	}


	/// <summary>
	/// 静态工厂方法：通过持久化数据字典创建一个新的对象实例。
	/// </summary>
	public static TimeSystem LoadSaveData(Dictionary<string, object> data)
	{
		float totalSeconds = 0.0f;
		if (data.ContainsKey("time"))
		{
			totalSeconds = Convert.ToSingle(data["time"]);
		}
		TimeSystem timeSystem = new TimeSystem(totalSeconds);
		return timeSystem;
	}

	/// <summary>
	/// 获取对象的持久化数据。
	/// </summary>
	public Dictionary<string, object> GetSaveData()
	{
		return new Dictionary<string, object>
		{
			{ "time", TotalSeconds }
		};
	}
}
