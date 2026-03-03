using Godot;

/// <summary>
/// 世界逻辑抽象类：负责增量的改变世界状态
/// </summary>
[GlobalClass]
public abstract partial class WorldLogic : RefCounted, IWorldLogic
{
	private float elapsedDays = 0.0f;
	protected float intervalDays = 1.0f;
	private float currentIntervalDays = 1.0f;
	private bool hasScheduledInterval = false;
	private float intervalJitterRatio = 0.15f;

	/// <summary>
	/// 世界逻辑名称（短文本）
	/// </summary>
	public string Name { get; protected set; } = string.Empty;

	/// <summary>
	/// 世界逻辑描述（固定文本）
	/// </summary>
	public string Description { get; protected set; } = string.Empty;

	/// <summary>
	/// 设置或获取执行间隔（天）
	/// </summary>
	public float IntervalDays
	{
		get => intervalDays;
		set
		{
			intervalDays = value;
			hasScheduledInterval = false;
		}
	}

	/// <summary>
	/// 设置或获取间隔随机偏移比例（0.0 - 0.95）
	/// 例如 0.15 表示每次按 ±15% 随机偏移
	/// </summary>
	public float IntervalJitterRatio
	{
		get => intervalJitterRatio;
		set
		{
			intervalJitterRatio = Mathf.Clamp(value, 0.0f, 0.95f);
			hasScheduledInterval = false;
		}
	}

	/// <summary>
	/// 获取当前累计时间（天）
	/// </summary>
	public float ElapsedDays => elapsedDays;

	/// <summary>
	/// 当逻辑触发时发送的信号
	/// </summary>
	[Signal]
	public delegate void TriggeredEventHandler(string name);

	/// <summary>
	/// 获取触发进度（0.0 - 1.0）
	/// </summary>
	public float ProgressRatio
	{
		get
		{
			if (intervalDays <= 0)
			{
				return 1.0f;
			}

			float targetInterval = GetCurrentIntervalDays();
			return Mathf.Clamp(elapsedDays / targetInterval, 0.0f, 1.0f);
		}
	}

	/// <summary>
	/// 获取剩余触发时间（天）
	/// </summary>
	public float RemainingDays
	{
		get
		{
			if (intervalDays <= 0)
			{
				return 0.0f;
			}

			return Mathf.Max(0.0f, GetCurrentIntervalDays() - elapsedDays);
		}
	}

	private float GetCurrentIntervalDays()
	{
		if (!hasScheduledInterval)
		{
			currentIntervalDays = GenerateNextIntervalDays();
			hasScheduledInterval = true;
		}

		return currentIntervalDays;
	}

	private float GenerateNextIntervalDays()
	{
		float baseInterval = Mathf.Max(0.0001f, intervalDays);
		if (intervalJitterRatio <= 0.0f)
		{
			return baseInterval;
		}

		double randomOffset = (System.Random.Shared.NextDouble() * 2.0 - 1.0) * intervalJitterRatio;
		float jitteredInterval = (float)(baseInterval * (1.0 + randomOffset));
		return Mathf.Max(0.0001f, jitteredInterval);
	}

	/// <summary>
	/// 返回触发进度（供 GDScript 直接调用）
	/// </summary>
	public float GetProgressRatio()
	{
		return ProgressRatio;
	}

	/// <summary>
	/// 每帧调用，处理内部计时器逻辑
	/// </summary>
	/// <param name="delta">时间增量</param>
	public virtual void UpdateDelta(float delta)
	{
		if (intervalDays <= 0)
		{
			ProcessLogic();
			return;
		}

		elapsedDays += delta / TimeSystem.SecondsPerDay;
		float targetInterval = GetCurrentIntervalDays();
		while (elapsedDays >= targetInterval)
		{
			elapsedDays -= targetInterval;
			ProcessLogic();
			EmitSignal(SignalName.Triggered, Name);
			hasScheduledInterval = false;
			targetInterval = GetCurrentIntervalDays();
		}
	}

	/// <summary>
	/// 当满足时间间隔时执行的具体逻辑，子类需实现此方法
	/// </summary>
	protected abstract void ProcessLogic();
}
