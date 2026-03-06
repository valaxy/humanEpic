using Godot;

/// <summary>
/// BuildingInfoPanelUI 组件演示入口。
/// </summary>
[GlobalClass]
public partial class BuildingInfoPanelUIDemo : Control
{
	// 建筑信息侧边栏。
	private BuildingInfoPanelUI buildingInfoPanelUi = null!;
	// 民宅建筑样例。
	private Building residentialBuilding = null!;
	// 市场建筑样例。
	private Building marketBuilding = null!;

	/// <summary>
	/// 初始化 Demo。
	/// </summary>
	public override void _Ready()
	{
		buildingInfoPanelUi = GetNode<BuildingInfoPanelUI>("BuildingInfoPanelUI");

		Button showResidentialButton = GetNode<Button>("ToolbarMargin/Toolbar/ShowResidentialButton");
		Button showMarketButton = GetNode<Button>("ToolbarMargin/Toolbar/ShowMarketButton");
		Button hideButton = GetNode<Button>("ToolbarMargin/Toolbar/HideButton");

		setupDemoData();

		showResidentialButton.Pressed += () => buildingInfoPanelUi.RenderBuilding(residentialBuilding);
		showMarketButton.Pressed += () => buildingInfoPanelUi.RenderBuilding(marketBuilding);
		hideButton.Pressed += () => buildingInfoPanelUi.Visible = false;

		buildingInfoPanelUi.RenderBuilding(residentialBuilding);
	}

	// 初始化演示用建筑数据。
	private void setupDemoData()
	{
		Country demoCountry = new Country("演示国家", Colors.CornflowerBlue);
		residentialBuilding = new Building(BuildingTemplate.GetTemplate(BuildingType.Enums.Residential), new Vector2I(1, 1), demoCountry);
		marketBuilding = new Building(BuildingTemplate.GetTemplate(BuildingType.Enums.Market), new Vector2I(2, 1), demoCountry);
	}
}
