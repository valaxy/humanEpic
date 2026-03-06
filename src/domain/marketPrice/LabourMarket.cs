using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 劳动力市场，统一管理各职业的日工资价格。
/// </summary>
public class LabourMarket
{
	private const string initialDt = "INIT";

	/// <summary>
	/// 劳动力市场变更事件。
	/// </summary>
	public event Action? LabourMarketChanged;

	// 劳动力日薪。
	public MarketDataBucket<JobType.Enums> JobPrices { get; }

	// 各职业供应量。
	public MarketDataBucket<JobType.Enums> JobSupplies { get; }

	// 各职业需求量。
	public MarketDataBucket<JobType.Enums> JobDemands { get; }

	// 劳动力价格历史。
	public PriceHistory<JobType.Enums> PriceHistory { get; }

	public LabourMarket()
	{
		IEnumerable<JobType.Enums> jobTypes = JobTemplate.GetTemplates().Keys;
		JobPrices = new MarketDataBucket<JobType.Enums>(jobTypes, _ => getRandomInitialPrice());
		JobSupplies = new MarketDataBucket<JobType.Enums>(jobTypes, _ => 0.0f);
		JobDemands = new MarketDataBucket<JobType.Enums>(jobTypes, _ => 0.0f);
		PriceHistory = new PriceHistory<JobType.Enums>();
		recordPriceSnapshot(initialDt);
	}

	private float getRandomInitialPrice()
	{
		double randomValue = Random.Shared.NextDouble();
		return 5.0f + (float)(randomValue * 5.0);
	}

	/// <summary>
	/// 基于供需关系，动态平衡劳动力价格
	/// 默认按照供需方向增加或减少当前价格的5%
	/// </summary>
	public void BalancePrice(string dt)
	{
		List<JobType.Enums> jobTypes = JobTemplate.GetTemplates().Keys.ToList();
		jobTypes.ForEach(jobType =>
		{
			float supply = JobSupplies.Get(jobType);
			float demand = JobDemands.Get(jobType);

			if (supply == 0 && demand == 0)
			{
				return;
			}

			float currentPrice = JobPrices.Get(jobType);
			float adjustment = 0.05f;

			if (demand > supply)
			{
				// 供不应求，涨价
				JobPrices.Set(jobType, currentPrice * (1.0f + adjustment));
			}
			else if (supply > demand)
			{
				// 供过于求，降价
				JobPrices.Set(jobType, currentPrice * (1.0f - adjustment));
			}
		});

		recordPriceSnapshot(dt);

		NotifyChanged();
	}

	/// <summary>
	/// 重置为单条价格历史记录。
	/// </summary>
	public void ResetPriceHistory(string dt)
	{
		PriceHistory.ResetWithSingleSnapshot(dt, captureCurrentPrices());
	}

	/// <summary>
	/// 从存档恢复价格历史。
	/// </summary>
	public void LoadPriceHistory(List<Dictionary<string, object>> historyData)
	{
		PriceHistory.LoadSaveData(historyData);
	}

	private void recordPriceSnapshot(string dt)
	{
		PriceHistory.Record(dt, captureCurrentPrices());
	}

	private Dictionary<JobType.Enums, float> captureCurrentPrices()
	{
		return JobTemplate.GetTemplates().Keys.ToDictionary(jobType => jobType, jobType => JobPrices.Get(jobType));
	}

	/// <summary>
	/// 手动触发劳动力市场变更通知。
	/// </summary>
	public void NotifyChanged()
	{
		LabourMarketChanged?.Invoke();
	}
}