using Godot;

/// <summary>
/// 游戏编辑器右上角工具组。
/// </summary>
[GlobalClass]
public partial class GameEditorTopRightTools : VBoxContainer
{
	// 网格按钮。
	private GridHelperButton gridButton = null!;

	// 保存按钮。
	private SaveButtonUI saveButton = null!;

	// 世界逻辑面板开关按钮。
	private EditorButton worldLogicButton = null!;

	// 百科面板开关按钮。
	private EditorButton encyclopediaButton = null!;

	// 世界逻辑状态面板。
	private WorldLogicStatusUI worldLogicStatusUi = null!;

	// 百科面板。
	private EncyclopediaPanelUI encyclopediaPanelUi = null!;

	public override void _Ready()
	{
		gridButton = GetNode<GridHelperButton>("RightTopContainer/GridButton");
		saveButton = GetNode<SaveButtonUI>("RightTopContainer/SaveButton");
		worldLogicButton = GetNode<EditorButton>("RightTopContainer/WorldLogicButton");
		encyclopediaButton = GetNode<EditorButton>("RightTopContainer/EncyclopediaButton");
		worldLogicStatusUi = GetNode<WorldLogicStatusUI>("WorldLogicStatusUI");
		encyclopediaPanelUi = GetNode<EncyclopediaPanelUI>("EncyclopediaPanelUI");

		worldLogicButton.Pressed += onWorldLogicPressed;
		encyclopediaButton.Pressed += onEncyclopediaPressed;
	}

	/// <summary>
	/// 初始化右上角工具组依赖。
	/// </summary>
	public void Setup(GameWorld world, GroundGridHelper gridRender, Simulation simulation)
	{
		gridButton.Setup(gridRender);
		saveButton.Setup(world);
		worldLogicStatusUi.Setup(simulation);
		encyclopediaPanelUi.Setup();
		worldLogicStatusUi.Visible = false;
		encyclopediaPanelUi.Visible = false;
		worldLogicButton.IsActive = false;
		encyclopediaButton.IsActive = false;
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
