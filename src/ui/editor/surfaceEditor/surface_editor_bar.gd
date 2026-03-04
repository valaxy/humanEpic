## 地表编辑器 UI 条
## 提供地表材质（草地、沙漠等）和地表高度（平原、山地等）的选择切换功能
extends EditorWindow

var current_surface: SurfaceType.Enums = SurfaceType.Enums.GRASSLAND ## 当前选中的地表材质类型
var current_height: TerrainHeight.Enums = TerrainHeight.Enums.PLAIN ## 当前选中的高度分层类型

var surface_buttons: Dictionary = {} ## 材质 ID 到按钮的映射
var height_buttons: Dictionary = {} ## 高度 ID 到按钮的映射

var _ground: GroundNode ## 地理节点引用
var _ui_manager: MainUI ## UI 管理器引用

@onready var surface_row: HBoxContainer = %TerrainRow ## 材质选择按钮布局容器
@onready var height_row: HBoxContainer = %HeightRow ## 高度选择按钮布局容器
@onready var brush_settings: HBoxContainer = %BrushForm ## 笔刷设置 UI 组件

var type_button_scene: PackedScene = preload("res://src/ui/base/type_button.tscn") ## 类型选择按钮原型

func setup(ground: GroundNode, ui_manager: MainUI) -> void:
	self._ground = ground
	self._ui_manager = ui_manager
	
	# 初始触发同步状态到逻辑层
	_trigger_selection()
	_trigger_brush_size(brush_settings.get_brush_size())

func _ready():
	super._ready()
	%TitleLabel.text = "地表编辑"
	setup_buttons()
	brush_settings.brush_size_changed.connect(on_brush_size_changed)

## 构建地表材质与高度的所有交互按钮
func setup_buttons() -> void:
	surface_buttons.clear()
	for type in SurfaceType.Enums.values():
		var template = SurfaceTemplate.GetTemplate(type)
		var btn = create_type_button(
			template.Name,
			template.Color,
			on_surface_selected.bind(type)
		)
		surface_row.add_child(btn)
		surface_buttons[type] = btn
	
	height_buttons.clear()
	for type in TerrainHeight.Enums.values():
		var template = TerrainHeightTemplate.GetTemplate(type)
		var btn = create_type_button(
			template.Name,
			template.Color,
			on_height_selected.bind(type)
		)
		height_row.add_child(btn)
		height_buttons[type] = btn
	
	update_selection_visuals()

## 处理笔刷大小数值变动的信号回调
func on_brush_size_changed(value: float) -> void:
	_trigger_brush_size(int(value))

## 同步笔刷大小到逻辑层和其他 UI
func _trigger_brush_size(size: int) -> void:
	if _ground:
		_ground.SetBrushSize(size)
	
	# 同步其他编辑器的笔刷 UI 状态
	if is_instance_valid(_ui_manager) and is_instance_valid(_ui_manager.overlay_editor) and _ui_manager.overlay_editor.has_node("%BrushForm"):
		_ui_manager.overlay_editor.get_node("%BrushForm").set_brush_size(size)

## 同步当前选中的材质和高度到地理逻辑层
func _trigger_selection() -> void:
	if _ground:
		_ground.SetSelectedTypes(current_surface, OverlayType.Enums.NONE, current_height)

## 工厂方法：创建一个带有文本和背景色块的按钮实例
func create_type_button(text: String, color: Color, callback: Callable) -> Button:
	var btn = type_button_scene.instantiate()
	btn.get_node("%Label").text = text
	btn.get_node("%ColorRect").color = color
	btn.pressed.connect(callback)
	return btn

## 同步当前选中的材质与高度对应的按钮边框高亮
func update_selection_visuals() -> void:
	for type in surface_buttons:
		var btn = surface_buttons[type]
		btn.get_node("%SelectionBorder").visible = (type == current_surface)
	
	for type in height_buttons:
		var btn = height_buttons[type]
		btn.get_node("%SelectionBorder").visible = (type == current_height)

## 响应并分发地表材质选择的行为
func on_surface_selected(type: SurfaceType.Enums) -> void:
	self.current_surface = type
	update_selection_visuals()
	_trigger_selection()

## 响应并分发地表高度分层选择的行为
func on_height_selected(type: TerrainHeight.Enums) -> void:
	self.current_height = type
	update_selection_visuals()
	_trigger_selection()
