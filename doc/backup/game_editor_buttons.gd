## 游戏编辑器 UI 按钮组件
## 处理编辑模式切换、按钮状态刷新及与其他模块的交互逻辑
extends CanvasLayer
class_name GameEditorButtons

var economy_editor_enabled: bool = false ## 资源覆盖物编辑器是否开启
var surface_editor_enabled: bool = false ## 地表/地形编辑器是否开启
var building_editor_enabled: bool = false ## 建筑编辑器是否开启
var current_selected_building: String = "" ## 当前待放置的建筑类型 ID
var current_selected_country_id: int = -1 ## 当前待放置建筑所属国家 ID

var ground_node: GroundNode ## 地理渲染管理节点引用
var ui_manager: MainUI ## 主 UI 管理器引用
var is_in_strategy_view: bool = false ## 是否处于大地图/战略视图模式下

@onready var terrain_button: Button = %TerrainButton
@onready var toggle_button: Button = %ToggleButton
@onready var build_button: Button = %BuildButton
@onready var grid_button: GridHelperButton = %GridButton
@onready var world_logic_button: Button = %WorldLogicButton
@onready var encyclopedia_button: Button = %EncyclopediaButton

@onready var time_display_ui: Node = %TimeDisplayUI
@onready var world_logic_status_ui: Node = %WorldLogicStatusUI
@onready var encyclopedia_panel_ui: EncyclopediaPanelUI = %EncyclopediaPanelUI
@onready var bottom_container: Control = %RightBottomContainer
@onready var save_button: SaveButtonUI = %SaveButton

func _ready() -> void:
	terrain_button.pressed.connect(toggle_surface_editor)
	toggle_button.pressed.connect(toggle_economy_editor)
	build_button.pressed.connect(toggle_building_editor)
	world_logic_button.pressed.connect(toggle_world_logic_panel)
	encyclopedia_button.pressed.connect(toggle_encyclopedia_panel)

## 初始化控制器并绑定核心逻辑组件
func setup(world: GameWorld, ground_node: GroundNode, ui_manager: MainUI, grid_render: Node) -> void:
	self.ground_node = ground_node
	self.ui_manager = ui_manager
	
	save_button.setup(world)
	time_display_ui.setup(world.TimeSystem)
	world_logic_status_ui.setup(world.Simulation)
	encyclopedia_panel_ui.setup()
	grid_button.setup(grid_render)
	
	update_visibility()

## 切换经济资源（覆盖物）编辑器的开启状态
func toggle_economy_editor() -> void:
	economy_editor_enabled = !economy_editor_enabled
	if economy_editor_enabled:
		surface_editor_enabled = false
		building_editor_enabled = false
		ground_node.IsUpdatingGround = true
	update_visibility()

## 切换地表（地形高度/表面材质）编辑器的开启状态
func toggle_surface_editor() -> void:
	surface_editor_enabled = !surface_editor_enabled
	if surface_editor_enabled:
		economy_editor_enabled = false
		building_editor_enabled = false
		ground_node.IsUpdatingGround = true
	update_visibility()

## 切换建筑放置编辑器的开启状态
func toggle_building_editor() -> void:
	building_editor_enabled = !building_editor_enabled
	if building_editor_enabled:
		economy_editor_enabled = false
		surface_editor_enabled = false
		ground_node.IsUpdatingGround = false
		ground_node.SetBrushSize(1) # 默认使用 1x1 笔刷放置建筑
		# 确保当前选中建筑不是空的
		if current_selected_building == "" and ui_manager.building_editor_bar:
			current_selected_building = ui_manager.building_editor_bar.current_building
		if current_selected_country_id < 0 and ui_manager.building_editor_bar:
			current_selected_country_id = ui_manager.building_editor_bar.selected_country_id
	else:
		ground_node.IsUpdatingGround = true
	update_visibility()

## 切换产品库存面板的可见性状态
func toggle_world_logic_panel() -> void:
	if not world_logic_status_ui:
		return

	world_logic_status_ui.visible = !world_logic_status_ui.visible
	if world_logic_button is EditorButton:
		world_logic_button.is_active = world_logic_status_ui.visible

## 切换百科全书面板可见性状态
func toggle_encyclopedia_panel() -> void:
	if not encyclopedia_panel_ui:
		return

	encyclopedia_panel_ui.visible = !encyclopedia_panel_ui.visible
	if encyclopedia_button is EditorButton:
		encyclopedia_button.is_active = encyclopedia_panel_ui.visible

## 更新编辑器和 UI 的整体可见性状态
func update_visibility() -> void:
	ground_node.SetActive(not is_in_strategy_view)
	
	var show_economy: bool = economy_editor_enabled and not is_in_strategy_view
	var show_surface: bool = surface_editor_enabled and not is_in_strategy_view
	var show_building: bool = building_editor_enabled and not is_in_strategy_view
	var any_editor_active: bool = show_economy or show_surface or show_building
	
	# 更新编辑器逻辑状态
	ground_node.SetCanDraw(any_editor_active)
	ground_node.IsOverlayMode = show_economy
	ground_node.SetBuildingEditorActive(show_building, current_selected_building, current_selected_country_id)
	
	# 更新 UI 面板可见性 (注意：这些面板仍在 ui_manager 下持有)
	ui_manager.overlay_editor.visible = show_economy
	ui_manager.surface_editor_bar.visible = show_surface
	ui_manager.building_editor_bar.visible = show_building
	
	# 更新按钮激活样式
	if terrain_button is EditorButton: terrain_button.is_active = show_surface
	if toggle_button is EditorButton: toggle_button.is_active = show_economy
	if build_button is EditorButton: build_button.is_active = show_building
	
	# 整体 UI 的显隐 (战略视图隐藏)
	visible = not is_in_strategy_view
	bottom_container.visible = not any_editor_active
	
	if is_in_strategy_view and world_logic_status_ui:
		world_logic_status_ui.visible = false
		if world_logic_button is EditorButton:
			world_logic_button.is_active = false

	if is_in_strategy_view and encyclopedia_panel_ui:
		encyclopedia_panel_ui.visible = false
		if encyclopedia_button is EditorButton:
			encyclopedia_button.is_active = false

## 监听战略视图切换事件，并在大地图视图下自动隐藏编辑器功能
func on_strategy_view_changed(is_strategy: bool) -> void:
	is_in_strategy_view = is_strategy
	update_visibility()

## 响应建筑选择事件，更新当前待放置的建筑类型
func on_building_selected(type: String) -> void:
	current_selected_building = type
	ground_node.SetSelectedTypes(ground_node.SelectedSurface, OverlayType.Enums.NONE, ground_node.SelectedHeight)
	ground_node.SetBuildingEditorActive(building_editor_enabled, current_selected_building, current_selected_country_id)

## 响应国家选择事件，更新当前待放置建筑所属国家
func on_country_selected(country_id: int) -> void:
	current_selected_country_id = country_id
	ground_node.SetBuildingEditorActive(building_editor_enabled, current_selected_building, current_selected_country_id)
