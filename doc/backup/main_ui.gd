# ## 主 UI 总管理器
# ## 集中负责所有 UI 组件的实例化、布局关联、信号绑定以及各级功能面板的开关控制逻辑
# extends Node
# class_name MainUI

# # ---------------------------------------------------------
# # 场景资源预加载
# # ---------------------------------------------------------
# var zoom_ui_scene: PackedScene = preload("res://src/ui/zoom_ui/zoom_ui.tscn") ## 缩放 UI 场景资源
# var overlay_editor_scene: PackedScene = preload("res://src/ui/editor/overlay_editor/overlay_editor.tscn") ## 覆盖物编辑场景资源
# var surface_editor_bar_scene: PackedScene = preload("res://src/ui/editor/surface_editor/surface_editor_bar.tscn") ## 地表编辑条场景资源
# var building_editor_bar_scene: PackedScene = preload("res://src/ui/editor/building_editor/building_editor_bar.tscn") ## 建筑编辑条场景资源
# var game_editor_buttons_scene: PackedScene = preload("res://src/ui/editor/game_editor_buttons.tscn") ## 编辑器按钮组件场景资源
# var info_ui_scene: PackedScene = preload("res://src/ui/info_ui/info_ui.tscn") ## 基础信息面板场景资源
# var product_ui_scene: PackedScene = preload("res://src/ui/product_ui/product_ui.tscn") ## 产品库存 UI 场景资源
# var bubble_container_scene: PackedScene = preload("res://src/ui/bubble_message/bubble_message_container_ui.tscn") ## 气泡消息容器场景资源

# # ---------------------------------------------------------
# # UI 逻辑实例引用
# # ---------------------------------------------------------
# var zoom_ui: ZoomUI ## 缩放控制 UI 实例
# var overlay_editor: Node ## 覆盖物（资源）编辑器实例
# var surface_editor_bar: EditorWindow ## 地表/地形编辑器实例
# var building_editor_bar: EditorWindow ## 建筑选择编辑器实例
# var product_ui: ProductUI ## 产品库存面板实例
# var game_editor_buttons: GameEditorButtons ## 编辑器 UI 按钮组实例
# var info_ui_left: InfoUI ## 左侧信息展示 UI 实例
# var info_ui_right: InfoUI ## 右侧信息展示 UI 实例
# var grid_info_ui: GridInfoUI ## 地块业务信息控制器
# var building_info_ui: BuildingInfoUI ## 建筑业务信息控制器
# var unit_info_ui: UnitInfoUI ## 单位业务信息控制器
# var grid_area_ui: GridAreaUI ## 地理连通区域展示控制器
# var bubble_container: BubbleMessageContainerUI ## 消息推送系统容器实例

# # ---------------------------------------------------------
# # 全局常用属性引用
# # ---------------------------------------------------------
# var world_logic_status_ui: Node ## 世界逻辑状态面板详情

# ## 全局初始化函数，负责实例化 UI 层级并跨节点组合各个逻辑模块
# func setup(world: GameWorld, ground_node: GroundNode, grid_area_node: GridAreaNode, selection_node: Node, camera: GameCameraNode, layer_manager: LayerManagerNode) -> void:
# 	# 1. 实例化
# 	overlay_editor = overlay_editor_scene.instantiate()
# 	add_child(overlay_editor)
# 	overlay_editor.visible = false
	
# 	surface_editor_bar = surface_editor_bar_scene.instantiate()
# 	add_child(surface_editor_bar)
# 	surface_editor_bar.visible = false
	
# 	building_editor_bar = building_editor_bar_scene.instantiate()
# 	add_child(building_editor_bar)
# 	building_editor_bar.visible = false


# 	selection_node.MarketBuildingSelected.connect(func(building: MarketBuilding):
# 		product_ui.bind_market(building.ProductMarket, building.LabourMarket)
# 		product_ui.visible = true
# 	)
# 	selection_node.ResidentialBuildingSelected.connect(func(_building: ResidentialBuilding): product_ui.visible = false)
# 	selection_node.IndustryBuildingSelected.connect(func(_building: IndustryBuilding): product_ui.visible = false)
# 	selection_node.HarvestBuildingSelected.connect(func(_building: HarvestBuilding): product_ui.visible = false)
# 	selection_node.CellSelected.connect(func(_pos: Vector2i): product_ui.visible = false)
# 	selection_node.SelectionCleared.connect(func(): product_ui.visible = false)
	
# 	game_editor_buttons = game_editor_buttons_scene.instantiate()
# 	game_editor_buttons.ui_manager = self
# 	game_editor_buttons.ground_node = ground_node
# 	add_child(game_editor_buttons)
	
# 	zoom_ui = zoom_ui_scene.instantiate()
# 	add_child(zoom_ui)
	
# 	info_ui_left = info_ui_scene.instantiate()
# 	add_child(info_ui_left)
# 	info_ui_left.set_position_offset(0)
	
# 	info_ui_right = info_ui_scene.instantiate()
# 	add_child(info_ui_right)
# 	info_ui_right.set_position_offset(310) # 300 width + 10 gap
	

# 	bubble_container = bubble_container_scene.instantiate()
# 	add_child(bubble_container)
	
# 	for logic in world.Simulation.GetWorldLogics():
# 		logic.Triggered.connect(bubble_container.add_message)
	
# 	# 2. 状态映射
# 	world_logic_status_ui = game_editor_buttons.world_logic_status_ui
	
# 	# 3. 交互逻辑关联
# 	zoom_ui.setup(camera, layer_manager, game_editor_buttons)
