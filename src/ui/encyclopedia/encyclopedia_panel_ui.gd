## 百科全书面板 UI
## 左侧展示词条列表，右侧按需加载并展示词条内容，避免一次性加载全部文本占用内存
extends PanelContainer
class_name EncyclopediaPanelUI

var entries: Array[Dictionary] = [
	{"title" = "世界逻辑系统", "path" = "res://src/data/encyclopedia/世界逻辑系统.txt"},
	{"title" = "覆盖物与地表编辑", "path" = "res://src/data/encyclopedia/覆盖物与地表编辑.txt"},
	{"title" = "建筑生产与劳动力", "path" = "res://src/data/encyclopedia/建筑生产与劳动力.txt"},
]

@onready var entry_list: ItemList = %EntryList
@onready var entry_title: Label = %EntryTitle
@onready var entry_content: RichTextLabel = %EntryContent

func _ready() -> void:
	entry_list.item_selected.connect(_on_entry_selected)

## 初始化词条列表，仅加载元信息，不预加载正文
func setup() -> void:
	entry_list.clear()
	for entry in entries:
		entry_list.add_item(entry["title"])
	entry_title.text = "词条内容"
	entry_content.text = "请选择左侧词条进行查看。"

## 响应词条点击事件，并动态读取对应文本内容
func _on_entry_selected(index: int) -> void:
	if index < 0 or index >= entries.size():
		return
	_load_entry_content(index)

## 按需从文本文件读取词条正文，点击时才加载以节省内存
func _load_entry_content(index: int) -> void:
	var entry: Dictionary = entries[index]
	var title: String = entry["title"]
	var file_path: String = entry["path"]

	entry_title.text = title

	if not FileAccess.file_exists(file_path):
		entry_content.text = "词条文件不存在：%s" % file_path
		return

	var file: FileAccess = FileAccess.open(file_path, FileAccess.READ)
	if file == null:
		entry_content.text = "词条加载失败：%s" % file_path
		return

	entry_content.text = file.get_as_text()
	file.close()
