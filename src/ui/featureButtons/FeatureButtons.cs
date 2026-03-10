using Godot;

/// <summary>
/// 顶层功能按钮组。
/// </summary>
[GlobalClass]
public partial class FeatureButtons : VBoxContainer
{
	// 网格按钮。
	private GridHelperButton gridButton = null!;

	// 保存按钮。
	private SaveButton saveButton = null!;

	// 人口窗口按钮。
	private EditorButton populationButton = null!;

	// 世界逻辑面板开关按钮。
	private EditorButton worldLogicButton = null!;

	// 百科面板开关按钮。
	private EditorButton encyclopediaButton = null!;

	// 世界逻辑状态面板。
	private WorldLogicStatusUI worldLogicStatusUi = null!;

	// 百科面板。
	private EncyclopediaPanelUI encyclopediaPanelUi = null!;

	// 人口窗口。
	private PopulationWindowUI populationWindowUi = null!;

	public override void _Ready()
	{
		gridButton = GetNode<GridHelperButton>("RightTopContainer/GridButton");
		saveButton = GetNode<SaveButton>("RightTopContainer/SaveButton");
		populationButton = GetNode<EditorButton>("RightTopContainer/PopulationButton");
		worldLogicButton = GetNode<EditorButton>("RightTopContainer/WorldLogicButton");
		encyclopediaButton = GetNode<EditorButton>("RightTopContainer/EncyclopediaButton");
		worldLogicStatusUi = GetNode<WorldLogicStatusUI>("WorldLogicStatusUI");
		encyclopediaPanelUi = GetNode<EncyclopediaPanelUI>("EncyclopediaPanelUI");
		populationWindowUi = GetNode<PopulationWindowUI>("PopulationWindowUI");

		populationButton.Pressed += onPopulationPressed;
		populationWindowUi.WindowVisibilityChanged += onPopulationWindowVisibilityChanged;
		worldLogicButton.Pressed += onWorldLogicPressed;
		encyclopediaButton.Pressed += onEncyclopediaPressed;
	}

	/// <summary>
	/// 初始化功能按钮组依赖。
	/// </summary>
	public void Setup(GameWorld world, GroundGridHelper gridRender, Simulation simulation)
	{
		gridButton.Setup(gridRender);
		saveButton.Setup(world);
		populationWindowUi.Setup(world);
		worldLogicStatusUi.Setup(simulation);
		encyclopediaPanelUi.Setup();
		populationWindowUi.SetWindowVisible(false);
		populationButton.IsActive = false;
		worldLogicStatusUi.Visible = false;
		encyclopediaPanelUi.Visible = false;
		worldLogicButton.IsActive = false;
		encyclopediaButton.IsActive = false;
	}

	// 切换人口窗口显示。
	private void onPopulationPressed()
	{
		populationWindowUi.SetWindowVisible(!populationWindowUi.IsWindowVisible);
	}

	// 同步人口按钮激活状态。
	private void onPopulationWindowVisibilityChanged(bool visible)
	{
		populationButton.IsActive = visible;
	}

	// 切换世界逻辑状态面板显示。
	private void onWorldLogicPressed()
	{
		bool targetVisible = !worldLogicStatusUi.Visible;
		worldLogicStatusUi.Visible = targetVisible;
		worldLogicButton.IsActive = targetVisible;
		if (targetVisible)
		{
			encyclopediaPanelUi.Visible = false;
			encyclopediaButton.IsActive = false;
		}
	}

	// 切换百科面板显示。
	private void onEncyclopediaPressed()
	{
		bool targetVisible = !encyclopediaPanelUi.Visible;
		encyclopediaPanelUi.Visible = targetVisible;
		encyclopediaButton.IsActive = targetVisible;
		if (targetVisible)
		{
			worldLogicStatusUi.Visible = false;
			worldLogicButton.IsActive = false;
		}
	}
}