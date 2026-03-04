using Godot;

/// <summary>
/// 游戏编辑器按钮总入口。
/// </summary>
[GlobalClass]
public partial class GameEditorButtons : CanvasLayer
{
	// 右上角工具组。
	private GameEditorTopRightTools topRightTools = null!;
	// 覆盖物编辑开关按钮。
	private EditorButton overlayButton = null!;
	// 覆盖物编辑器。
	private OverlayEditor overlayEditor = null!;

	public override void _Ready()
	{
		topRightTools = GetNode<GameEditorTopRightTools>("Control/TopRightTools");
		overlayButton = GetNode<EditorButton>("Control/RightBottomContainer/ToggleButton");
		overlayEditor = GetNode<OverlayEditor>("Control/OverlayEditor");

		overlayButton.Pressed += onOverlayButtonPressed;
		overlayEditor.CloseRequested += onOverlayEditorClosed;
	}

	/// <summary>
	/// 初始化编辑器按钮区域。
	/// </summary>
	public void Setup(GameWorld world, GameView view, Simulation simulation)
	{
		topRightTools.Setup(world, view.GridRender, simulation);
		overlayEditor.Setup(world.Ground, view.LayerManager, view.GroundView);
		overlayEditor.SetEditorVisible(false);
		overlayButton.IsActive = false;
	}

	// 切换覆盖物编辑器显隐。
	private void onOverlayButtonPressed()
	{
		bool targetVisible = !overlayEditor.Visible;
		overlayEditor.SetEditorVisible(targetVisible);
		overlayButton.IsActive = targetVisible;
	}

	// 响应编辑器关闭并同步按钮状态。
	private void onOverlayEditorClosed()
	{
		overlayButton.IsActive = false;
	}
}
