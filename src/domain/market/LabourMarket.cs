/// <summary>
/// 劳动力市场，统一管理各职业价格与供需。
/// </summary>
[Persistable]
public class LabourMarket : Market<JobType.Enums>
{
	public LabourMarket() : base(JobTemplate.GetTemplates().Keys) { }
}
