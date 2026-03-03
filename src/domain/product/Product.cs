/// <summary>
/// 代表对产品的具体持有
/// </summary>
public class Product
{
	/// <summary>
	/// 产品模板
	/// </summary>
	public ProductTemplate Template { get; }

	/// <summary>
	/// 当前持有数量
	/// </summary>
	public int Amount { get; set; }

	/// <summary>
	/// 初始化产品持有
	/// </summary>
	/// <param name="template">产品模板</param>
	/// <param name="amount">初始数量</param>
	public Product(ProductTemplate template, int amount)
	{
		Template = template;
		Amount = amount;
	}
}
