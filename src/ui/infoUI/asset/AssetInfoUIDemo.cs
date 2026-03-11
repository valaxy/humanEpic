using System.Collections.Generic;
using Godot;

/// <summary>
/// Asset 信息面板演示入口。
/// </summary>
[GlobalClass]
public partial class AssetInfoUIDemo : Control
{
	// 通用信息面板。
	private InfoUI infoUi = null!;

	// 当前演示资产对象。
	private Asset asset = null!;

	// 展示基础资产按钮。
	private Button showBasicButton = null!;

	// 展示丰富资产按钮。
	private Button showRichButton = null!;

	// 展示空资产按钮。
	private Button showEmptyButton = null!;

	// 隐藏面板按钮。
	private Button hideButton = null!;

	/// <summary>
	/// 初始化 Demo 并默认展示基础资产。
	/// </summary>
	public override void _Ready()
	{
		infoUi = GetNode<InfoUI>("InfoUI");
		showBasicButton = GetNode<Button>("ToolbarMargin/Toolbar/ShowBasicButton");
		showRichButton = GetNode<Button>("ToolbarMargin/Toolbar/ShowRichButton");
		showEmptyButton = GetNode<Button>("ToolbarMargin/Toolbar/ShowEmptyButton");
		hideButton = GetNode<Button>("ToolbarMargin/Toolbar/HideButton");

		infoUi.SetPositionOffset(20.0f);

		showBasicButton.Pressed += showBasicAsset;
		showRichButton.Pressed += showRichAsset;
		showEmptyButton.Pressed += showEmptyAsset;
		hideButton.Pressed += () => infoUi.HideInfo();

		showBasicAsset();
	}

	// 展示基础资产样例。
	private void showBasicAsset()
	{
		asset = new Asset(new Dictionary<ProductType.Enums, float>
		{
			{ ProductType.Enums.BREAD, 120.0f },
			{ ProductType.Enums.BLUEBERRY, 200.0f },
			{ ProductType.Enums.CURRENCY, 320.0f },
		});

		showAssetInfo("资产信息 Demo（基础）");
	}

	// 展示丰富资产样例。
	private void showRichAsset()
	{
		asset = new Asset(new Dictionary<ProductType.Enums, float>
		{
			{ ProductType.Enums.BREAD, 860.0f },
			{ ProductType.Enums.BLUEBERRY, 910.0f },
			{ ProductType.Enums.TOY, 120.0f },
			{ ProductType.Enums.BARBECUE, 64.0f },
			{ ProductType.Enums.BOOK, 48.0f },
			{ ProductType.Enums.WOOD, 240.0f },
			{ ProductType.Enums.STONE, 430.0f },
			{ ProductType.Enums.ORE, 130.0f },
			{ ProductType.Enums.TOOLS, 72.0f },
			{ ProductType.Enums.FURNITURE, 36.0f },
			{ ProductType.Enums.CURRENCY, 1580.0f },
		});

		showAssetInfo("资产信息 Demo（丰富）");
	}

	// 展示空资产样例。
	private void showEmptyAsset()
	{
		asset = new Asset(new());
		showAssetInfo("资产信息 Demo（空资产）");
	}

	// 将资产信息展示到信息面板。
	private void showAssetInfo(string title)
	{
		infoUi.ShowInfo(title, asset.GetInfoData());
	}
}
