/// <summary>
/// 需求类型枚举
/// </summary>
public static class DemandType
{
	public enum Enums
	{
		/// <summary>
		/// 食物需求，是基础的生存需求，是最底层、最迫切的需求
		/// 与生育率挂钩
		/// 饱和生存型效用
		/// </summary>
		FOOD = 0,

		/// <summary>
		/// 娱乐需求，影响幸福感、安全感
		/// 无底洞型效用
		/// </summary>
		ENTERTAINMENT = 1,

		/// <summary>
		/// 教育需求，影响生产力
		/// 阶梯跨越型
		/// </summary>
		EDUCATION = 2,

		/// <summary>
		/// 精神/宗教需求，影响宗教信仰，提高忠诚度
		/// 阶梯跨越型
		/// </summary>
		SPIRITUAL = 3,
	}
}
