using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 建筑编辑器国家选择组件。
/// 负责渲染国家下拉框并向外发出国家变更事件。
/// </summary>
[GlobalClass]
public partial class CountrySelector : HBoxContainer
{
	/// <summary>
	/// 国家选中变化信号。
	/// </summary>
	[Signal]
	public delegate void CountrySelectedEventHandler(int countryId);

	// 当前选中的国家 ID。
	public int SelectedCountryId { get; private set; } = -1;

	// 国家下拉框。
	private OptionButton countryOptionButton = null!;

	// 组件内置国家数据。
	private readonly List<Country> localCountries =
	[
		new Country("晨曦邦", Colors.CornflowerBlue, 1),
		new Country("赤岩领", Colors.IndianRed, 2),
		new Country("霜叶国", Colors.SeaGreen, 3),
		new Country("银湾城", Colors.Goldenrod, 4)
	];

	public override void _Ready()
	{
		countryOptionButton = GetNode<OptionButton>("%CountryOptionButton");
		countryOptionButton.ItemSelected += onCountryOptionSelected;
	}

	/// <summary>
	/// 初始化国家选择下拉框。
	/// </summary>
	public void Setup()
	{
		countryOptionButton.Clear();

		localCountries
			.OrderBy(country => country.Id)
			.ToList()
			.ForEach(country => countryOptionButton.AddItem(country.Name, country.Id));

		if (countryOptionButton.ItemCount == 0)
		{
			SelectedCountryId = -1;
			return;
		}

		countryOptionButton.Select(0);
		SelectedCountryId = countryOptionButton.GetItemId(0);
		EmitSignal(SignalName.CountrySelected, SelectedCountryId);
	}

	// 处理国家下拉选中变化。
	private void onCountryOptionSelected(long index)
	{
		SelectedCountryId = countryOptionButton.GetItemId((int)index);
		EmitSignal(SignalName.CountrySelected, SelectedCountryId);
	}
}
