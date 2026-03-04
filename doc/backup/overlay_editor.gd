## 覆盖物编辑器 UI
## 提供不同地块覆盖物（如资源等）的选择和笔刷大小设置功能
extends EditorWindow
class_name OverlayEditor

var currentOverlayType: OverlayType.Enums = OverlayType.Enums.NONE ## 当前选中的覆盖物类型 ID
var overlayButtons: Dictionary = {} ## 覆盖物类型到按钮实例的映射字典

@onready var overlayRow: HBoxContainer = %OverlayRow ## 存放覆盖物选择按钮的水平容器
@onready var brushSettings: HBoxContainer = %BrushSettingsUI ## 笔刷设置 UI 组件

var typeButtonScene: PackedScene = preload("res://src/ui/base/type_button.tscn") ## 类型选择按钮场景资源

var groundNode: GroundNode ## 地面节点引用
var editorController: GameEditorButtons ## 编辑器控制器引用
var mainUIManager: MainUI ## UI 管理器引用

## 初始化编辑器关联的对象并连接信号
func setup(ground: GroundNode, controller: GameEditorButtons, ui_manager: MainUI) -> void:
	groundNode = ground
	editorController = controller
	mainUIManager = ui_manager
	close_requested.connect(func(): editorController.toggle_economy_editor())
	
	# 初始化状态同步
	triggerSelection()
	triggerBrushSize(brushSettings.get_brush_size())

func _ready():
	super._ready()
	%TitleLabel.text = "覆盖物编辑"
	setupButtons()
	setupSignals()

## 连接并处理子控件发出的各类交互信号
func setupSignals() -> void:
	brushSettings.brush_size_changed.connect(onBrushSizeChanged)

## 根据覆盖物模板动态生成并布局所有可用的覆盖物选择按钮
func setupButtons() -> void:
	overlayButtons.clear()
	for child in overlayRow.get_children():
		child.queue_free()
	
	var templates = OverlayTemplate.GetTemplates_AsGDictionary()
	for type in templates:
		var template = templates[type]
		var btn = createTypeButton(
			template.Name, 
			template.Color,
			onOverlaySelected.bind(type)
		)
		overlayRow.add_child(btn)
		overlayButtons[type] = btn
	
	updateSelectionVisuals()

## 响应笔刷大小数值变更的信号处理器
func onBrushSizeChanged(value: int) -> void:
	triggerBrushSize(value)

## 同步笔刷大小到地理逻辑层和其他编辑器
func triggerBrushSize(size: int) -> void:
	if groundNode:
		groundNode.SetBrushSize(size)
	
	# 同步其他编辑器的笔刷 UI 状态
	if is_instance_valid(mainUIManager) and is_instance_valid(mainUIManager.surface_editor_bar) and mainUIManager.surface_editor_bar.has_node("%BrushSettingsUI"):
		mainUIManager.surface_editor_bar.get_node("%BrushSettingsUI").set_brush_size(size)

## 工厂方法：创建一个带有标签和颜色标识的类型选择按钮
func createTypeButton(text: String, color: Color, callback: Callable) -> Button:
	var btn = typeButtonScene.instantiate()
	btn.get_node("%Label").text = text
	btn.get_node("%ColorRect").color = color
	btn.pressed.connect(callback)
	return btn

## 刷新所有按钮的选中状态边框显示
func updateSelectionVisuals() -> void:
	for type in overlayButtons:
		var btn = overlayButtons[type]
		btn.get_node("%SelectionBorder").visible = (type == currentOverlayType)

## 处理具体的覆盖物类型选择逻辑并分发状态更新
func onOverlaySelected(overlayType: OverlayType.Enums) -> void:
	currentOverlayType = overlayType
	updateSelectionVisuals()
	triggerSelection()

## 同步当前选中的覆盖物类型到地理逻辑层
func triggerSelection() -> void:
	if groundNode:
		groundNode.SetSelectedTypes(groundNode.SelectedSurface, currentOverlayType, groundNode.SelectedHeight)
