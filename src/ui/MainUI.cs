using Godot;

/// <summary>
/// 主 UI 管理器，负责核心 UI 组件初始化与装配。
/// </summary>
[GlobalClass]
public partial class MainUI : Node
{
	// 缩放 UI 实例。
	private ZoomUI zoomUi = null!;
	// 时间显示 UI 实例。
	private TimeDisplayUI timeDisplayUi = null!;
	// 左侧地格信息面板。
	private InfoUI infoUiLeft = null!;
	// 右侧覆盖物信息面板。
	private InfoUI infoUiRight = null!;
	// 地格信息控制器。
	private GridInfoUI gridInfoUi = null!;

	public override void _Ready()
	{
		zoomUi = GetNode<ZoomUI>("ZoomUI");
		timeDisplayUi = GetNode<TimeDisplayUI>("TimeDisplayUI");
		infoUiLeft = GetNode<InfoUI>("InfoUILeft");
		infoUiRight = GetNode<InfoUI>("InfoUIRight");
		gridInfoUi = GetNode<GridInfoUI>("GridInfoUI");
	}

	/// <summary>
	/// 初始化主 UI。
	/// </summary>
	public void Setup(GameWorld world, GameCamera camera, LayerManagerNode layerManager, GroundSelection selection)
	{
		zoomUi.Setup(camera, layerManager);
		timeDisplayUi.Setup(world.TimeSystem);

		infoUiLeft.SetPositionOffset(0.0f);
		infoUiRight.SetPositionOffset(310.0f);
		gridInfoUi.Setup(infoUiLeft, infoUiRight, world.Ground);

		selection.CellHovered += gridInfoUi.OnCellHovered;
		selection.CellHoverCleared += gridInfoUi.OnCellHoverCleared;
		selection.CellSelected += gridInfoUi.OnCellSelected;
		selection.SelectionCleared += gridInfoUi.OnSelectionCleared;

		// 以下为旧版 GDScript 中尚未迁移完成的业务 UI，先保留注释占位。
		// var overlay_editor_scene: PackedScene = preload("res://src/ui/editor/overlay_editor/overlay_editor.tscn")
		// var surface_editor_bar_scene: PackedScene = preload("res://src/ui/editor/surface_editor/surface_editor_bar.tscn")
		// var building_editor_bar_scene: PackedScene = preload("res://src/ui/editor/building_editor/building_editor_bar.tscn")
		// var game_editor_buttons_scene: PackedScene = preload("res://src/ui/editor/game_editor_buttons.tscn")
		// var info_ui_scene: PackedScene = preload("res://src/ui/info_ui/info_ui.tscn")
		// var product_ui_scene: PackedScene = preload("res://src/ui/product_ui/product_ui.tscn")
		// var bubble_container_scene: PackedScene = preload("res://src/ui/bubble_message/bubble_message_container_ui.tscn")
		// TODO: 对应 C# 类型与场景迁移后，再逐步恢复这些功能。
	}
}
