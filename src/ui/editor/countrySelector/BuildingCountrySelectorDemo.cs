using Godot;

/// <summary>
/// BuildingCountrySelector 组件演示入口。
/// </summary>
[GlobalClass]
public partial class BuildingCountrySelectorDemo : Control
{
	// 国家选择器组件。
	private BuildingCountrySelector countrySelector = null!;

	// 状态文本。
	private Label statusLabel = null!;

	public override void _Ready()
	{
		countrySelector = GetNode<BuildingCountrySelector>("%CountrySelector");
		statusLabel = GetNode<Label>("%StatusLabel");

		GameWorld world = GameWorldInitializer.Load();
		countrySelector.Setup(world.Countries);
		countrySelector.CountrySelected += onCountrySelected;

		onCountrySelected(countrySelector.SelectedCountryId);
	}

	// 响应国家切换并展示结果。
	private void onCountrySelected(int countryId)
	{
		statusLabel.Text = countryId < 0 ? "未找到国家数据" : $"当前国家 ID: {countryId}";
	}
}
