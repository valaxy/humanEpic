using Godot;

/// <summary>
/// 百科面板演示入口。
/// </summary>
[GlobalClass]
public partial class EncyclopediaPanelUIDemo : Node
{
	// 百科面板实例。
	private EncyclopediaPanelUI encyclopediaPanelUi = null!;

	public override void _Ready()
	{
		encyclopediaPanelUi = GetNode<EncyclopediaPanelUI>("EncyclopediaPanelUI");
		encyclopediaPanelUi.Setup();
	}
}
