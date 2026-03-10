using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 建筑工具条 UI，用于选择并放置建筑。
/// </summary>
[GlobalClass]
public partial class BuildingEditorBar : EditorWindow
{
	// 类型按钮场景。
	private static readonly PackedScene TypeButtonScene = GD.Load<PackedScene>("res://src/ui/editor/type_button.tscn");

	/// <summary>
	/// 建筑类型选中变化信号。
	/// </summary>
	[Signal]
	public delegate void BuildingSelectedEventHandler(int buildingTypeId);

	/// <summary>
	/// 国家选中变化信号。
	/// </summary>
	[Signal]
	public delegate void CountrySelectedEventHandler(int countryId);

	// 当前选中的建筑类型。
	private BuildingType.Enums currentBuildingType = BuildingType.Enums.Residential;

	// 建筑按钮映射。
	private readonly Dictionary<BuildingType.Enums, Button> buildingButtons = new();

	// 建筑按钮容器。
	private HBoxContainer buildingRow = null!;

	// 国家选择组件。
	private CountrySelector countrySelector = null!;

	// 笔刷对象。
	private Brush brush = null!;

	// 世界模型。
	private GameWorld world = null!;

	// 视图入口。
	private GameView view = null!;

	// 地图编辑器。
	private GroundEditView groundEditor = null!;

	public override void _Ready()
	{
		base._Ready();
		buildingRow = GetNode<HBoxContainer>("%BuildingRow");
		countrySelector = GetNode<CountrySelector>("%CountrySelector");
		GetNode<Label>("%TitleLabel").Text = "建筑建造";

		setupButtons();
		countrySelector.CountrySelected += onCountrySelected;
	}

	/// <summary>
	/// 初始化编辑器依赖并绑定地图点击事件。
	/// </summary>
	public void Setup(GameWorld worldRef, GameView viewRef, GroundEditView groundEditorRef, Brush brushRef)
	{
		world = worldRef;
		view = viewRef;
		groundEditor = groundEditorRef;
		brush = brushRef;

		groundEditor.EditCellRequested -= onGroundCellClicked;
		groundEditor.EditCellRequested += onGroundCellClicked;

		countrySelector.Setup(worldRef.Countries);
		selectDefaultBuildingType();
	}

	/// <summary>
	/// 设置编辑器显隐状态。
	/// </summary>
	public void SetEditorVisible(bool visible)
	{
		Visible = visible;
		if (visible)
		{
			brush.SetSize(1);
		}

		groundEditor.SetBuildingMode(visible, currentBuildingType);
	}

	// 动态生成建筑类型按钮。
	private void setupButtons()
	{
		buildingButtons.Clear();
		buildingRow.GetChildren().ToList().ForEach(child => child.QueueFree());

		BuildingTemplate.GetTemplates()
			.OrderBy(item => item.Key)
			.ToList()
			.ForEach(item =>
			{
				Button button = createTypeButton(item.Value.Name, item.Value.Color, item.Key);
				buildingRow.AddChild(button);
				buildingButtons[item.Key] = button;
			});

		updateSelectionVisuals();
	}

	// 创建建筑类型按钮。
	private Button createTypeButton(string text, Color color, BuildingType.Enums buildingType)
	{
		Button button = TypeButtonScene.Instantiate<Button>();
		button.GetNode<Label>("%Label").Text = text;
		button.GetNode<ColorRect>("%ColorRect").Color = color;
		button.Pressed += () => onBuildingSelected(buildingType);
		return button;
	}

	// 刷新选中态视觉。
	private void updateSelectionVisuals()
	{
		buildingButtons.Keys.ToList().ForEach(type =>
		{
			Button button = buildingButtons[type];
			button.GetNode<ReferenceRect>("%SelectionBorder").Visible = type == currentBuildingType;
		});
	}

	// 选中默认建筑类型。
	private void selectDefaultBuildingType()
	{
		if (buildingButtons.Count == 0)
		{
			return;
		}

		currentBuildingType = buildingButtons.Keys.OrderBy(type => type).First();
		updateSelectionVisuals();
		EmitSignal(SignalName.BuildingSelected, (int)currentBuildingType);
		groundEditor.SetBuildingMode(Visible, currentBuildingType);
	}

	// 响应建筑类型切换。
	private void onBuildingSelected(BuildingType.Enums buildingType)
	{
		currentBuildingType = buildingType;
		updateSelectionVisuals();
		EmitSignal(SignalName.BuildingSelected, (int)currentBuildingType);
		groundEditor.SetBuildingMode(Visible, currentBuildingType);
	}

	// 响应国家选择变化。
	private void onCountrySelected(int countryId)
	{
		EmitSignal(SignalName.CountrySelected, countryId);
	}

	// 响应地图点击并执行建筑放置。
	private void onGroundCellClicked(Vector2I cellPos)
	{
		if (!Visible)
		{
			return;
		}

		if (countrySelector.SelectedCountryId < 0)
		{
			return;
		}

		if (!world.Ground.IsInsideGround(cellPos) || world.Buildings.HasKeyByPos(cellPos))
		{
			return;
		}

		Country country = world.Countries.Get(countrySelector.SelectedCountryId);
		BuildingTemplate template = BuildingTemplate.GetTemplate(currentBuildingType);
		Building building = new Building(template, cellPos, country);
		world.Buildings.Add(building);
		assignRandomResidents(building);
		view.BuildingCollection.UpdateBuildingVisuals();
	}

	// 新建民宅时，随机生成一些人口入住进去，可以不住满或者住满
	private void assignRandomResidents(Building building)
	{
		if (building.Residential == null)
		{
			return;
		}

		List<Population> availablePopulations = world.Populations.GetAll()
			.Where(population => population.Count > population.PopulationResidential.TotalPopCount)
			.ToList();

		if (availablePopulations.Count == 0)
		{
			return;
		}

		int maxAssignable = Math.Min(
			building.Residential.OptimalCount,
			availablePopulations.Sum(population => population.Count - population.PopulationResidential.TotalPopCount));
		if (maxAssignable <= 0)
		{
			return;
		}

		int targetResidentCount = (int)GD.RandRange(0, maxAssignable);
		Enumerable.Range(0, targetResidentCount)
			.ToList()
			.ForEach(_ =>
			{
				List<Population> currentAvailable = availablePopulations
					.Where(population => population.Count > population.PopulationResidential.TotalPopCount)
					.ToList();

				if (currentAvailable.Count == 0)
				{
					return;
				}

				int randomIndex = (int)GD.RandRange(0, currentAvailable.Count - 1);
				Population selected = currentAvailable[randomIndex];
				selected.PopulationResidential.Birth(building, 1);
			});
	}
}
