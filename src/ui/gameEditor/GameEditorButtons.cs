using Godot;

/// <summary>
/// 游戏编辑器按钮总入口。
/// </summary>
[GlobalClass]
public partial class GameEditorButtons : CanvasLayer
{
	// 右上角工具组。
	private GameEditorTopRightTools topRightTools = null!;
	// 地表编辑开关按钮。
	private EditorButton terrainButton = null!;
	// 覆盖物编辑开关按钮。
	private EditorButton overlayButton = null!;
	// 地表编辑器。
	private SurfaceEditorBar surfaceEditor = null!;
	// 覆盖物编辑器。
	private OverlayEditor overlayEditor = null!;
	// 右下角按钮组容器。
	private HBoxContainer rightBottomContainer = null!;
	// 地图交互入口。
	private GroundView groundView = null!;
	// 地图编辑器。
	private GroundEditor groundEditor = null!;

	private enum ActiveEditorMode
	{
		None,
		Surface,
		Overlay
	}

	public override void _Ready()
	{
		topRightTools = GetNode<GameEditorTopRightTools>("Control/TopRightTools");
		rightBottomContainer = GetNode<HBoxContainer>("Control/RightBottomContainer");
		terrainButton = GetNode<EditorButton>("Control/RightBottomContainer/TerrainButton");
		overlayButton = GetNode<EditorButton>("Control/RightBottomContainer/ToggleButton");
		surfaceEditor = GetNode<SurfaceEditorBar>("Control/SurfaceEditor");
		overlayEditor = GetNode<OverlayEditor>("Control/OverlayEditor");

		terrainButton.Pressed += onTerrainButtonPressed;
		overlayButton.Pressed += onOverlayButtonPressed;
		surfaceEditor.CloseRequested += onSurfaceEditorClosed;
		overlayEditor.CloseRequested += onOverlayEditorClosed;
	}

	/// <summary>
	/// 初始化编辑器按钮区域。
	/// </summary>
	public void Setup(GameWorld world, GameView view, Simulation simulation)
	{
		groundView = view.GroundView;
		groundEditor = groundView.GetEditor();
		Brush brush = groundView.GetBrush();
		topRightTools.Setup(world, view.GridRender, simulation);
		surfaceEditor.Setup(world.Ground, view.LayerManager, view.GroundView, groundEditor, brush);
		overlayEditor.Setup(world.Ground, view.LayerManager, view.GroundView, groundEditor, brush);
		switchEditor(ActiveEditorMode.None);
	}

	// 切换地表编辑器显隐。
	private void onTerrainButtonPressed()
	{
		switchEditor(surfaceEditor.Visible ? ActiveEditorMode.None : ActiveEditorMode.Surface);
	}

	// 切换覆盖物编辑器显隐。
	private void onOverlayButtonPressed()
	{
		switchEditor(overlayEditor.Visible ? ActiveEditorMode.None : ActiveEditorMode.Overlay);
	}

	// 统一切换编辑器状态。
	private void switchEditor(ActiveEditorMode mode)
	{
		bool isSurfaceVisible = mode == ActiveEditorMode.Surface;
		bool isOverlayVisible = mode == ActiveEditorMode.Overlay;

		surfaceEditor.SetEditorVisible(isSurfaceVisible);
		overlayEditor.SetEditorVisible(isOverlayVisible);
		rightBottomContainer.Visible = !isSurfaceVisible && !isOverlayVisible;

		terrainButton.IsActive = isSurfaceVisible;
		overlayButton.IsActive = isOverlayVisible;

		if (!isSurfaceVisible && !isOverlayVisible)
		{
			groundEditor.DisableEditMode();
		}
	}

	// 响应地表编辑器关闭并同步按钮状态。
	private void onSurfaceEditorClosed()
	{
		switchEditor(ActiveEditorMode.None);
	}

	// 响应编辑器关闭并同步按钮状态。
	private void onOverlayEditorClosed()
	{
		switchEditor(ActiveEditorMode.None);
	}
}
