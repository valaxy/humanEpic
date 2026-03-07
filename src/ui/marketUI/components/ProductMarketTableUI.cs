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
	// 标题标签。
	private Label titleLabel = null!;

	// 数据网格容器。
	private GridContainer dataGrid = null!;

	// 当前选中商品。
	private ProductType.Enums? selectedProductType;

	// 普通行单元格样式。
	private static readonly StyleBoxFlat normalCellStyle = new()
	{
		BgColor = new Color(0f, 0f, 0f, 0f),
		ContentMarginLeft = 8,
		ContentMarginRight = 8,
		ContentMarginTop = 4,
		ContentMarginBottom = 4
	};

	// 选中行单元格样式。
	private static readonly StyleBoxFlat selectedCellStyle = new()
	{
		BgColor = new Color(0.95f, 0.84f, 0.43f, 0.9f),
		ContentMarginLeft = 8,
		ContentMarginRight = 8,
		ContentMarginTop = 4,
		ContentMarginBottom = 4
	};

	/// <summary>
	/// 点击商品行时触发。
	/// </summary>
	public event Action<ProductType.Enums>? ProductRowPressed;

	/// <summary>
	/// 初始化节点引用。
	/// </summary>
	public override void _Ready()
	{
		base._Ready();
		titleLabel = GetNode<Label>("%TitleLabel");
		dataGrid = GetNode<GridContainer>("%DataGrid");
	}

	/// <summary>
	/// 根据产品市场数据刷新表格。
	/// </summary>
	public void RenderMarket(ProductMarket market, ProductType.Enums? selectedProductType = null)
	{
		this.selectedProductType = selectedProductType;

		List<(ProductType.Enums type, List<string> columns)> rows = Enum.GetValues<ProductType.Enums>()
			.Select(type =>
			{
				ProductTemplate template = ProductTemplate.GetTemplate(type);
				float demandAmount = market.ConsumerDemands.Get(type) + market.IndustryDemands.Get(type);
				float outputAmount = market.Supplies.Get(type);
				float price = market.Prices.Get(type);
				return (type, new List<string>
				{
					template.Name,
					$"{price:0.00}",
					$"{demandAmount:0.00}",
					$"{outputAmount:0.00}"
				});
			})
			.ToList();

		titleLabel.Text = "商品市场";
		dataGrid.Columns = 4;
		clearGrid();
		addHeaders();
		addRows(rows);
	}

	// 添加表头。
	private void addHeaders()
	{
		List<(string text, HorizontalAlignment alignment)> headers =
		[
			("商品", HorizontalAlignment.Left),
			("价格", HorizontalAlignment.Right),
			("需求量", HorizontalAlignment.Right),
			("供应量", HorizontalAlignment.Right)
		];

		headers
			.ToList()
			.ForEach(header =>
			{
				Label headerLabel = new Label
				{
					Text = header.text,
					HorizontalAlignment = header.alignment
				};
				dataGrid.AddChild(headerLabel);
			});
	}

	// 添加数据行。
	private void addRows(List<(ProductType.Enums type, List<string> columns)> rows)
	{
		rows
			.ToList()
			.ForEach(row =>
			{
				bool isSelected = selectedProductType.HasValue && selectedProductType.Value == row.type;
				List<HorizontalAlignment> alignments =
				[
					HorizontalAlignment.Left,
					HorizontalAlignment.Right,
					HorizontalAlignment.Right,
					HorizontalAlignment.Right
				];

				Enumerable.Range(0, row.columns.Count)
					.ToList()
					.ForEach(columnIndex =>
					{
						Button cellButton = createDataCellButton(
							row.columns[columnIndex],
							alignments[columnIndex],
							isSelected,
							row.type);
						dataGrid.AddChild(cellButton);
					});
			});
	}

	// 清空网格单元格。
	private void clearGrid()
	{
		List<Node> children = dataGrid.GetChildren().Cast<Node>().ToList();
		children.ForEach(child => child.QueueFree());
	}

	// 创建可点击的数据单元格。
	private Button createDataCellButton(string text, HorizontalAlignment alignment, bool isSelected, ProductType.Enums productType)
	{
		Button button = new Button
		{
			Text = text,
			Flat = true,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			Alignment = alignment,
			FocusMode = Control.FocusModeEnum.None,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		button.AddThemeStyleboxOverride("normal", isSelected ? selectedCellStyle : normalCellStyle);
		button.AddThemeStyleboxOverride("hover", isSelected ? selectedCellStyle : normalCellStyle);
		button.AddThemeStyleboxOverride("pressed", selectedCellStyle);
		button.AddThemeStyleboxOverride("focus", isSelected ? selectedCellStyle : normalCellStyle);
		button.Pressed += () => ProductRowPressed?.Invoke(productType);
		return button;
	}
}
