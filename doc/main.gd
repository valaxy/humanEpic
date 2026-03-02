# ## 3D 场景主逻辑，处理摄像机和地图渲染与交互
# extends Node3D

# @export var default_map_width: int = 500 ## 默认地图宽度
# @export var default_map_height: int = 500 ## 默认地图高度

# @onready var camera: GameCameraNode = $Camera3D ## 主摄像机节点
# @onready var natural_disaster_manager = NaturalDisasterManager.new() ## 管理自然灾害渲染的管理器
# @onready var main_ui = MainUI.new() ## 管理界面 UI 的管理器
# @onready var view = GameViewNode.new() ## 视图层顶层入口

# var world: GameWorld = GameWorld.new() ## 领域层顶层入口

# func _ready():
# 	var start_time = Time.get_ticks_msec()
	
# 	add_child(view)
# 	add_child(natural_disaster_manager)
# 	add_child(main_ui)
	
# 	# 初始化视图层
# 	var view_setup_start = Time.get_ticks_msec()
# 	view.Setup(camera, world)
# 	print("[Perf] GameView setup: %d ms" % (Time.get_ticks_msec() - view_setup_start))

# 	natural_disaster_manager.register_collection(world.NaturalDisasters)
	
# 	var ui_setup_start = Time.get_ticks_msec()
# 	main_ui.setup(world, view.Ground, view.Ground.GridAreaRender, view.SelectionController, camera, view.LayerManager)
# 	print("[Perf] MainUI setup: %d ms" % (Time.get_ticks_msec() - ui_setup_start))

# 	# 初始化控制器
# 	main_ui.game_editor_buttons.setup(world, view.Ground, main_ui, view.GridRender)

# 	# 初始化各类编辑器模块
# 	main_ui.overlay_editor.setup(view.Ground, main_ui.game_editor_buttons, main_ui)
# 	main_ui.surface_editor_bar.setup(view.Ground, main_ui.game_editor_buttons, main_ui)
	
# 	main_ui.building_editor_bar.building_selected.connect(func(type): main_ui.game_editor_buttons.on_building_selected(type))
# 	main_ui.building_editor_bar.country_selected.connect(func(country_id): main_ui.game_editor_buttons.on_country_selected(country_id))
# 	main_ui.building_editor_bar.close_requested.connect(func(): main_ui.game_editor_buttons.toggle_building_editor())
	
# 	# 领域信号连接
# 	EventHub.Instance().GroundCellsChanged.connect(func(cells): view.LayerManager.UpdateCells(cells, world.Ground))
# 	world.Simulation.MigrationLogic.MigrationTriggered.connect(func(start, count, target, country_id): view.UnitController.OnMigrationTriggered(start, count, target, country_id))
	
# 	# 地理信号连接
# 	view.Ground.MapUpdated.connect(func(): 
# 		view.LayerManager.UpdateMapData(world.Ground)
# 		if view.BuildingCollection:
# 			view.BuildingCollection.RefreshVisuals()
# 		view.UpdateGridVisuals(world.Ground.Width, world.Ground.Height)
# 	)
	
# 	# 使用 GameWorld 统一加载游戏状态
# 	var load_start = Time.get_ticks_msec()
# 	world.Load()
# 	print("[Perf] World Load (C# Logic): %d ms" % (Time.get_ticks_msec() - load_start))
	
# 	if world.Ground.Width > 0:
# 		print("Loaded map with size: ", world.Ground.Width, "x", world.Ground.Height)
		
# 		var visual_start = Time.get_ticks_msec()
# 		view.UpdateGridVisuals(world.Ground.Width, world.Ground.Height)
# 		print("[Perf] _update_grid_visuals: %d ms" % (Time.get_ticks_msec() - visual_start))
		
# 		var layer_start = Time.get_ticks_msec()
# 		view.LayerManager.UpdateMapData(world.Ground)
# 		print("[Perf] layer_manager.UpdateMapData: %d ms" % (Time.get_ticks_msec() - layer_start))
		
# 		var eco_refresh_start = Time.get_ticks_msec()
# 		view.BuildingCollection.RefreshVisuals()
# 		print("[Perf] building_collection_node.RefreshVisuals: %d ms" % (Time.get_ticks_msec() - eco_refresh_start))
# 	else:
# 		print("No map found, creating default %dx%d map" % [default_map_width, default_map_height])
# 		view.Ground.ResizeMap(default_map_width, default_map_height)
# 		view.BuildingCollection.RefreshVisuals()
		
# 	# 确保编辑器初始状态正确
# 	main_ui.game_editor_buttons.update_visibility()
	
# 	print("[Perf] Total main_3d _ready took: %d ms" % (Time.get_ticks_msec() - start_time))
# 	print("Hello World from 3D!")

# func _process(delta):
# 	world.TimeSystem.Update(delta)
# 	world.UnitCollection.UpdateCombat(delta)
# 	world.Simulation.Update(delta)
