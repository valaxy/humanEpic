using Godot;

/// <summary>
/// BuildingInfoUI 组件演示入口。
/// </summary>
[GlobalClass]
public partial class BuildingInfoUIDemo : Control
{
	// 建筑信息控制器。
	private BuildingInfoUI buildingInfoUi = null!;
	// 通用信息面板。
	private InfoUI infoUi = null!;
	// 演示建筑集合。
	private BuildingCollection buildingCollection = null!;

	/// <summary>
	/// 初始化 Demo。
	/// </summary>
	public override void _Ready()
	{
		buildingInfoUi = GetNode<BuildingInfoUI>("BuildingInfoUI");
		infoUi = GetNode<InfoUI>("InfoUI");

		Button showResidentialButton = GetNode<Button>("ToolbarMargin/Toolbar/ShowResidentialButton");
		Button showMarketButton = GetNode<Button>("ToolbarMargin/Toolbar/ShowMarketButton");
		Button clearButton = GetNode<Button>("ToolbarMargin/Toolbar/ClearButton");

		setupDemoData();
		infoUi.SetPositionOffset(20.0f);
		buildingInfoUi.Setup(buildingCollection, infoUi);

		showResidentialButton.Pressed += () => buildingInfoUi.OnCellSelected(new Vector2I(1, 1));
		showMarketButton.Pressed += () => buildingInfoUi.OnCellSelected(new Vector2I(2, 1));
		clearButton.Pressed += buildingInfoUi.OnSelectionCleared;
		buildingInfoUi.OnCellSelected(new Vector2I(1, 1));
	}

	// 初始化演示用建筑集合。
	private void setupDemoData()
	{
		GameWorld world = GameWorldInitializer.Load();

		CountryCollection countries = new CountryCollection();
		PopulationCollection populations = new PopulationCollection();
		Country demoCountry = new Country("演示国家", Colors.CornflowerBlue);
		countries.Add(demoCountry);

		buildingCollection = new BuildingCollection(world.Ground, countries, populations);
		buildingCollection.Add(new Building(BuildingTemplate.GetTemplate(BuildingType.Enums.Residential), new Vector2I(1, 1), demoCountry));
		buildingCollection.Add(new Building(BuildingTemplate.GetTemplate(BuildingType.Enums.Market), new Vector2I(2, 1), demoCountry));
	}
}
