/// <summary>
/// 定义两个指标的关系，表示输出关系
/// </summary>
public sealed class MetricRelation
{
	/// <summary>
	/// 计算过程中作为输入
	/// </summary>
	public Metric Input { get; }

	/// <summary>
	/// 计算过程汇总作为输出
	/// </summary>
	public Metric Output { get; }

	/// <summary>
	/// 创建有指标关系
	/// </summary>
	public MetricRelation(Metric input, Metric output)
	{
		Input = input;
		Output = output;
	}
}
