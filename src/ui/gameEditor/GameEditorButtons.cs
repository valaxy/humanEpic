using Godot;

/// <summary>
/// 游戏编辑器按钮总入口。
/// </summary>
[GlobalClass]
public partial class GameEditorButtons : CanvasLayer
{
	// 右上角工具组。
	private GameEditorTopRightTools topRightTools = null!;

	public override void _Ready()
	{
		topRightTools = GetNode<GameEditorTopRightTools>("Control/TopRightTools");
	}

	/// <summary>
	/// 初始化编辑器按钮区域。
	/// </summary>
	public void Setup(GameWorld world, GameView view, Simulation simulation)
	{
		topRightTools.Setup(world, view.GridRender, simulation);
	}
}
