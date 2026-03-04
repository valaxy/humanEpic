## 全局保存触发按钮
## 负责监听并收集游戏当前的逻辑与视觉数据，并指挥领域层执行序列化保存操作
extends Button
class_name SaveButtonUI

var world: GameWorld ## 顶级领域模型引用，用于执行保存逻辑

func _ready() -> void:
	pressed.connect(on_pressed)

## 为保存按钮注册数据源，使其获取触发保存的能力
func setup(world_data: GameWorld) -> void:
	self.world = world_data

## 响应并捕获点击行为，以此触发核心领域模型的保存过程
func on_pressed() -> void:
	if world:
		print("SaveButtonUI: 开始执行序列化保存...")
		world.Save()
	else:
		printerr("SaveButtonUI 错误: 领域模型引用为空，无法提交保存请求。")
