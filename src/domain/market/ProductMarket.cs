/// <summary>
/// 市场系统，统一管理每种产品的需求量、产出量和价格。
/// </summary>
[Persistable]
public class ProductMarket : Market<ProductType.Enums>
{
	public ProductMarket() : base(ProductTemplate.GetTemplates().Keys) { }
}
