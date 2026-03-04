## 世界逻辑状态监视面板
## 遍历并显示所有全局世界逻辑（如自然灾害、资源再生等）的名称、描述以及下一次触发的百分比进度
extends PanelContainer
class_name WorldLogicStatusUI

var simulation: Simulation ## 关联的世界模拟层根对象
var logic_rows: Array = [] ## 存放单行逻辑显示相关控件引用的缓存列表

@onready var list_container: VBoxContainer = %ListContainer ## 包含所有逻辑条目的滚动垂直容器

## 配置世界模拟引用并触发列表界面的初次构建
func setup(simulation_ref: Simulation) -> void:
	self.simulation = simulation_ref
	rebuild_rows()

func _process(_delta: float) -> void:
	if not visible:
		return
	update_progress()

## 读取当前 Simulation 中的所有逻辑对象并动态生成对应的 UI 内容行
func rebuild_rows() -> void:
	logic_rows.clear()
	if not list_container:
		return

	# 清理旧有的动态条目
	for child in list_container.get_children():
		child.queue_free()

	if not simulation:
		return

	var logics = simulation.GetWorldLogics()
	for logic in logics:
		var row: VBoxContainer = VBoxContainer.new()
		row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		list_container.add_child(row)

		var title_label: Label = Label.new()
		title_label.text = logic.Name
		title_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		title_label.tooltip_text = logic.Description
		row.add_child(title_label)

		var progress_row: HBoxContainer = HBoxContainer.new()
		progress_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(progress_row)

		var progress_bar: ProgressBar = ProgressBar.new()
		progress_bar.min_value = 0.0
		progress_bar.max_value = 1.0
		progress_bar.step = 0.001
		progress_bar.show_percentage = false
		progress_bar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		progress_bar.tooltip_text = logic.Description
		progress_row.add_child(progress_bar)

		var percent_label: Label = Label.new()
		percent_label.custom_minimum_size = Vector2(46, 0)
		percent_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		progress_row.add_child(percent_label)

		# 缓存对应的 UI 组件以便在 _process 中更新
		logic_rows.append({
			"logic" = logic,
			"bar" = progress_bar,
			"label" = percent_label,
		})

	update_progress()

## 核心刷新函数：计算并同步所有逻辑的最新阶段比率到进度条和文本上
func update_progress() -> void:
	for row in logic_rows:
		var logic = row["logic"]
		var progress_value: float = logic.GetProgressRatio()
		var progress_bar: ProgressBar = row["bar"]
		var percent_label: Label = row["label"]
		progress_bar.value = progress_value
		percent_label.text = "%d%%" % int(progress_value * 100.0)
