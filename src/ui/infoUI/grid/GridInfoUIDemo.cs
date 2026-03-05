using Godot;
using System;

/// <summary>
/// GridInfoUI 组件演示入口。
/// </summary>
[GlobalClass]
public partial class GridInfoUIDemo : Control
{
	// 地格信息控制器。
	private GridInfoUI gridInfoUi = null!;
	// 左侧信息面板。
	private InfoUI infoUiLeft = null!;
	// 右侧信息面板。
	private InfoUI infoUiRight = null!;
	// 演示用世界对象。
	private GameWorld world = null!;

	/// <summary>
	/// 初始化 Demo。
	/// </summary>
	public override void _Ready()
	{
		gridInfoUi = GetNode<GridInfoUI>("GridInfoUI");
		infoUiLeft = GetNode<InfoUI>("InfoUILeft");
		infoUiRight = GetNode<InfoUI>("InfoUIRight");

		Button hoverCenterButton = GetNode<Button>("ToolbarMargin/Toolbar/HoverCenterButton");
		Button hoverRandomButton = GetNode<Button>("ToolbarMargin/Toolbar/HoverRandomButton");
		Button clearButton = GetNode<Button>("ToolbarMargin/Toolbar/ClearButton");

		world = GameWorldInitializer.Load();
		infoUiLeft.SetPositionOffset(0.0f);
		infoUiRight.SetPositionOffset(310.0f);
		gridInfoUi.Setup(infoUiLeft, infoUiRight, world.Ground);

		hoverCenterButton.Pressed += showCenterCell;
		hoverRandomButton.Pressed += showRandomCell;
		clearButton.Pressed += gridInfoUi.OnCellHoverCleared;

		showCenterCell();
	}

	// 展示中心地格信息。
	private void showCenterCell()
	{
		Vector2I center = new Vector2I(world.Ground.Width / 2, world.Ground.Height / 2);
		gridInfoUi.OnCellHovered(center);
	}

	// 展示随机地格信息。
	private void showRandomCell()
	{
		int x = Random.Shared.Next(0, world.Ground.Width);
		int y = Random.Shared.Next(0, world.Ground.Height);
		gridInfoUi.OnCellHovered(new Vector2I(x, y));
	}
}
