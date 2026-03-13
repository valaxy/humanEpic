using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 资产条目，保存某一产品模板的持有数量。
/// </summary>
[Persistable]
public class AssetItem
{
	// 缓存，提高性能
	private ProductTemplate template = default!;

	// 数量。
	[PersistField]
	private float amount = default!;

	// 每日消费量（键为第几天）。
	[PersistField]
	private DailyHistoryData<float> consumedAmountHistory = new(30);

	/// <summary>
	/// 产品类型。
	/// </summary>
	[PersistProperty]
	public ProductType.Enums ProductType
	{
		get => template.Type;
		private set => template = ProductTemplate.GetTemplate(value);
	}


	/// <summary>
	/// 当前数量。
	/// </summary>
	public float Amount => amount;

	/// <summary>
	/// 对应的产品模板。
	/// </summary>
	public ProductTemplate Template => template;

	/// <summary>
	/// 读取指定天数的消费量。
	/// </summary>
	public float GetConsumedAmountByDay(int day)
	{
		return consumedAmountHistory.GetValueByDay(day);
	}

	/// <summary>
	/// 获取过去 30 天消费序列（不足补零）。
	/// </summary>
	public IReadOnlyList<float> GetLast30DaysConsumption(int currentDay)
	{
		return consumedAmountHistory.GetRecentValues(currentDay, 30).ToList();
	}

	/// <summary>
	/// 获取最近 X 天的平均消费量。
	/// </summary>
	public double GetRecentAverageConsumption(int currentDay, int recentDays)
	{
		return consumedAmountHistory.GetRecentAverage(currentDay, recentDays);
	}



	/// <summary>
	/// 无参构造函数，供反持久化调用。
	/// </summary>
	private AssetItem()
	{
	}

	/// <summary>
	/// 初始化资产条目。
	/// </summary>
	public AssetItem(ProductType.Enums productType, float amount)
	{
		ProductType = productType;
		SetAmount(amount);
	}

	/// <summary>
	/// 设置数量。
	/// </summary>
	public void SetAmount(float amount)
	{
		Debug.Assert(amount >= 0.0f, "资产数量不能为负数");
		this.amount = amount;
	}

	/// <summary>
	/// 增加数量。
	/// </summary>
	public void AddAmount(float amount)
	{
		Debug.Assert(amount > 0.0f, "增加资产数量不能为负数");
		SetAmount(this.amount + amount);
	}


	/// <summary>
	/// 消耗数量并记录到指定天数。
	/// </summary>
	public void ConsumeAmount(float amount, int day)
	{
		Debug.Assert(amount > 0.0f, "消耗资产数量不能为负数");
		Debug.Assert(this.amount >= amount, "资产数量不足，无法消耗");
		Debug.Assert(day >= 0);
		SetAmount(this.amount - amount);

		recordDailyConsumption(day, amount);
	}

	// 记录并裁剪 30 天窗口。
	private void recordDailyConsumption(int day, float consumed)
	{
		consumedAmountHistory.AddCurrentDayValue(day, consumed);
	}
}
