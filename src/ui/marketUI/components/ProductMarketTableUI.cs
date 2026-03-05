using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 产品市场表组件。
/// </summary>
[GlobalClass]
public partial class ProductMarketTableUI : ReusableDataTable
{
	/// <summary>
	/// 根据产品市场数据刷新表格。
	/// </summary>
	public void RenderMarket(ProductMarket market)
	{
		List<List<string>> rows = Enum.GetValues<ProductType.Enums>()
			.Select(type =>
			{
				ProductTemplate template = ProductTemplate.GetTemplate(type);
				float demandAmount = market.ConsumerDemands.Get(type) + market.IndustryDemands.Get(type);
				float outputAmount = market.Supplies.Get(type);
				float price = market.Prices.Get(type);
				return new List<string>
				{
					template.Name,
					$"{demandAmount:0.00}",
					$"{outputAmount:0.00}",
					$"{price:0.00}"
				};
			})
			.ToList();

		List<string> headers = ["产品", "需求量", "产出量", "价格"];
		List<DataTextAlignment> alignments =
		[
			DataTextAlignment.Left,
			DataTextAlignment.Right,
			DataTextAlignment.Right,
			DataTextAlignment.Right
		];

		DataSource source = DataSource.CreateTable("产品价格", headers, rows, alignments, alignments);
		Render(source);
	}
}
