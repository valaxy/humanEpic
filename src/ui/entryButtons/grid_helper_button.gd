## 网格辅助开关按钮
## 用于控制并同步场景中地平面辅助网格的显示与隐藏状态
extends EditorButton
class_name GridHelperButton

var grid_node: Node3D ## 被控制的地面网格节点

## 初始化并绑定待控制的地面网格对象
func setup(grid_node: Node3D) -> void:
	self.grid_node = grid_node
	if grid_node:
		is_active = grid_node.visible

func _ready() -> void:
	super._ready()
	pressed.connect(on_pressed)

## 响应按钮点击并切换网格的可见性状态
func on_pressed() -> void:
	if grid_node:
		var grid_visible: bool = !grid_node.visible
		grid_node.visible = grid_visible
		is_active = grid_visible
