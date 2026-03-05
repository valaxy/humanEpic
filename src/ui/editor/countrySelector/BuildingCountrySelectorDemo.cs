using Godot;

/// <summary>
/// CountrySelector 组件演示入口。
/// </summary>
[GlobalClass]
public partial class BuildingCountrySelectorDemo : Control
{
	// 国家选择器组件。
	private CountrySelector countrySelector = null!;

	// 状态文本。
	private Label statusLabel = null!;

	public override void _Ready()
	{
		countrySelector = GetNode<CountrySelector>("%CountrySelector");
		statusLabel = GetNode<Label>("%StatusLabel");

		countrySelector.Setup();
		countrySelector.CountrySelected += onCountrySelected;

		onCountrySelected(countrySelector.SelectedCountryId);
	}

	// 响应国家切换并展示结果。
	private void onCountrySelected(int countryId)
	{
		statusLabel.Text = countryId < 0 ? "未找到国家数据" : $"当前国家 ID: {countryId}";
	}
}
