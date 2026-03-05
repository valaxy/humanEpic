using Godot;

/// <summary>
/// 地格选中与悬浮行为管理器。
/// </summary>
[GlobalClass]
public partial class GroundSelection : Node
{
    private static readonly PackedScene GroundCursorScene = GD.Load<PackedScene>("res://src/view/selection/GroundCursor.tscn");

    // 世界数据引用。
    private GameWorld world = null!;
    // 地面交互入口引用。
    private GroundView groundView = null!;
    // 建筑渲染集合入口。
    private BuildingCollectionNode buildingCollectionNode = null!;
    // 地面选中光标。
    private GroundCursor groundCursor = null!;

    /// <summary>
    /// 当地格被选中时发出。
    /// </summary>
    [Signal]
    public delegate void CellSelectedEventHandler(Vector2I cellPos);

    /// <summary>
    /// 当鼠标悬浮地格时发出。
    /// </summary>
    [Signal]
    public delegate void CellHoveredEventHandler(Vector2I cellPos);

    /// <summary>
    /// 当鼠标离开地格时发出。
    /// </summary>
    [Signal]
    public delegate void CellHoverClearedEventHandler();

    /// <summary>
    /// 当取消选中时发出。
    /// </summary>
    [Signal]
    public delegate void SelectionClearedEventHandler();

    /// <summary>
    /// 当建筑被选中时发出。
    /// </summary>
    [Signal]
    public delegate void BuildingSelectedEventHandler(Vector2I cellPos);

    /// <summary>
    /// 当建筑选中被清理时发出。
    /// </summary>
    [Signal]
    public delegate void BuildingSelectionClearedEventHandler();

    /// <summary>
    /// 初始化选中管理器并绑定地面交互信号。
    /// </summary>
    public void Setup(GameWorld world, GroundView groundView, BuildingCollectionNode buildingCollectionNode)
    {
        this.world = world;
        this.groundView = groundView;
        this.buildingCollectionNode = buildingCollectionNode;

        groundCursor = GroundCursorScene.Instantiate<GroundCursor>();
        AddChild(groundCursor);

        groundView.CellClicked += onGroundCellClicked;
        groundView.CellHovered += onGroundCellHovered;
        groundView.CellHoverCleared += onGroundCellHoverCleared;
        buildingCollectionNode.BuildingClicked += onBuildingClicked;
    }

    // 处理地格点击行为。
    private void onGroundCellClicked(Vector2I cellPos)
    {
        ProcessSelection(cellPos);
    }

    // 处理建筑几何体点击行为。
    private void onBuildingClicked(Vector2I cellPos)
    {
        ProcessSelection(cellPos);
    }

    // 处理地格悬浮行为。
    private void onGroundCellHovered(Vector2I cellPos)
    {
        EmitSignal(SignalName.CellHovered, cellPos);
    }

    // 处理悬浮清理行为。
    private void onGroundCellHoverCleared()
    {
        EmitSignal(SignalName.CellHoverCleared);
    }

    /// <summary>
    /// 处理地格选中逻辑。
    /// </summary>
    public void ProcessSelection(Vector2I cellPos)
    {
        if (world.Ground.IsInsideGround(cellPos))
        {
            ShowCellSelection(cellPos);
            EmitSignal(SignalName.CellSelected, cellPos);

            if (world.Buildings.HasKey(cellPos))
            {
                Building building = world.Buildings.Get(cellPos);
                buildingCollectionNode.ShowBuildingVirtual(building);
                EmitSignal(SignalName.BuildingSelected, cellPos);
                return;
            }

            buildingCollectionNode.HideBuildingInfo();
            EmitSignal(SignalName.BuildingSelectionCleared);
            return;
        }

        ClearSelection();
    }

    /// <summary>
    /// 显示地格选中视觉效果。
    /// </summary>
    public void ShowCellSelection(Vector2I pos)
    {
        groundCursor.ShowCell(pos, world.Ground);
    }

    /// <summary>
    /// 清理选中状态。
    /// </summary>
    public void ClearSelection()
    {
        groundCursor.Clear();
        buildingCollectionNode.HideBuildingInfo();
        EmitSignal(SignalName.SelectionCleared);
        EmitSignal(SignalName.BuildingSelectionCleared);
    }
}



// TODO 历史遗留代码，仅供参考，先别删
// public override void _UnhandledInput(InputEvent @event)
// {
// 	if (world == null || geographyManager?.Camera == null || selectedUnits.Count == 0) return;

// 	// 只有在选中了单位的情况下，右键点击地面发出移动指令
// 	if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } mouseEvent)
// 	{
// 		handleGlobalMovement(mouseEvent.Position);
// 	}
// }

// /// <summary>
// /// 处理全局单位移动逻辑
// /// </summary>
// private void handleGlobalMovement(Vector2 mousePos)
// {
// 	Vector3? hitPos = geographyManager?.Camera?.ProjectToPlane(mousePos, YConfig.PlainY);
// 	if (hitPos is Vector3 hit && world != null)
// 	{
// 		Vector2 gridTargetFloat = world.Ground.WorldToGrid(hit);
// 		Vector2I gridTarget = new Vector2I((int)gridTargetFloat.X, (int)gridTargetFloat.Y);
// 		foreach (Unit unit in selectedUnits)
// 		{
// 			if (!unit.Fight.IsInCombat)
// 			{
// 				unit.CurrentAction = new MoveAction(unit, world.Ground, gridTarget);
// 				unit.EmitPathChanged();
// 			}
// 		}
// 	}
// }





// Building? building = world.Buildings.GetBuilding(cellPos);
// if (building != null)
// {
// 	OnGeographyBuildingSelected(building);

// 	switch (building)
// 	{
// 		case MarketBuilding market:
// 			EmitSignal(SignalName.MarketBuildingSelected, market);
// 			break;
// 		case ResidentialBuilding residential:
// 			EmitSignal(SignalName.ResidentialBuildingSelected, residential);
// 			break;
// 		case IndustryBuilding industry:
// 			EmitSignal(SignalName.IndustryBuildingSelected, industry);
// 			break;
// 		case HarvestBuilding harvest:
// 			EmitSignal(SignalName.HarvestBuildingSelected, harvest);
// 			break;
// 	}
// }
// else
// {

// }
// /// <summary>
// /// 清理所有选中状态
// /// </summary>
// /// <param name="keepUnit">是否保留单位选中状态</param>
// public void ClearSelection(bool keepUnit = false)
// {
// 	buildingCollectionNode?.HideBuildingInfo();
// 	buildingCollectionNode?.ClearRangeRender();
// 	geographyManager?.ClearAreaRender();
// 	SelectionMesh?.Clear();
// 	AttackRangeRender?.Clear();

// 	if (!keepUnit)
// 	{
// 		ClearSelectedUnits();
// 		EmitSignal(SignalName.SelectionCleared);
// 	}
// }


// public override void _ExitTree()
// {
// 	EventHub.Instance().UnitDead -= onUnitRemovedOrDead;
// 	EventHub.Instance().UnitPositionChanged -= onUnitPositionChanged;
// }

// private void onUnitPositionChanged(Unit unit)
// {
// 	if (selectedUnits.Contains(unit))
// 	{
// 		if (world != null && AttackRangeRender != null && unit.Fight?.AttackRange != null)
// 		{
// 			AttackRangeRender.UpdateByCenter(unit.CenterPoint, unit.Fight.AttackRange.Radius, world.Ground);
// 		}
// 	}
// }

// private void onUnitRemovedOrDead(Unit unit)
// {
// 	RemoveSelectedUnit(unit);
// }



// private Building? FindBuildingAt(Vector2I pos)
// {
// 	return world?.Buildings.GetBuilding(pos);
// }

// /// <summary>
// /// 初始化组件
// /// </summary>
// public void Setup(GameWorld world, GroundNode geographyManager, BuildingCollectionNode buildingCollectionNode, UnitController unitController)
// {
// 	this.world = world;
// 	this.geographyManager = geographyManager;
// 	this.buildingCollectionNode = buildingCollectionNode;
// 	this.unitController = unitController;



// 	if (AttackRangeRender == null)
// 	{
// 		AttackRangeRender = new CircularRangeNode();
// 		AddChild(AttackRangeRender);
// 		AttackRangeRender.Color = new Color(1.0f, 0.4f, 0.4f, 0.25f); // 攻击范围淡红色
// 	}

// 	// 监听单位移除/死亡信号，确保清理选中状态
// 	EventHub.Instance().UnitDead += onUnitRemovedOrDead;
// 	EventHub.Instance().UnitPositionChanged += onUnitPositionChanged;
// 	world.UnitCollection.Removed += (item) => { if (item is Unit unit) onUnitRemovedOrDead(unit); };
// 	world.WildlifeCollection.Removed += (item) => { if (item is Unit unit) onUnitRemovedOrDead(unit); };
// }



// /// <summary>攻击范围视觉显示</summary>
// public CircularRangeNode? AttackRangeRender { get; set; }

// private GroundNode? geographyManager;
// private BuildingCollectionNode? buildingCollectionNode;
// private UnitController? unitController;
// private readonly List<Unit> selectedUnits = new();


// /// <summary>
// /// 当单位不再被选中时发出
// /// </summary>
// [Signal] public delegate void UnitUnselectedEventHandler();

// /// <summary>
// /// 当特定单位不再被选中时发出
// /// </summary>
// [Signal] public delegate void UnitDeselectedEventHandler(Unit unit);


// /// <summary>
// /// 当市场建筑被选中时发出
// /// </summary>
// [Signal] public delegate void MarketBuildingSelectedEventHandler(MarketBuilding building);

// /// <summary>
// /// 当民宅被选中时发出
// /// </summary>
// [Signal] public delegate void ResidentialBuildingSelectedEventHandler(ResidentialBuilding building);

// /// <summary>
// /// 当工业建筑被选中时发出
// /// </summary>
// [Signal] public delegate void IndustryBuildingSelectedEventHandler(IndustryBuilding building);

// /// <summary>
// /// 当采集建筑被选中时发出
// /// </summary>
// [Signal] public delegate void HarvestBuildingSelectedEventHandler(HarvestBuilding building);

// /// <summary>
// /// 当单位被选中时发出
// /// </summary>
// [Signal] public delegate void UnitSelectedEventHandler(Unit unit);


// /// <summary>
// /// 处理单位节点的输入事件回调
// /// </summary>
// public void OnUnitInputEvent(Unit unit, InputEvent @event)
// {
// 	if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
// 	{
// 		GetViewport().SetInputAsHandled();
// 		OnUnitSelected(unit);
// 	}
// }

// /// <summary>
// /// 当单位被选中时的回调逻辑
// /// </summary>
// public void OnUnitSelected(Unit unit)
// {
// 	ClearSelection();
// 	AddSelectedUnit(unit);

// 	if (world != null && AttackRangeRender != null && unit.Fight?.AttackRange != null)
// 	{
// 		AttackRangeRender.UpdateByCenter(unit.CenterPoint, unit.Fight.AttackRange.Radius, world.Ground);
// 	}
// }

// /// <summary>
// /// 当单位取消选中时的回调逻辑
// /// </summary>
// public void OnUnitUnselected()
// {
// 	AttackRangeRender?.Clear();
// 	EmitSignal(SignalName.UnitUnselected);
// }



// /// <summary>
// /// 处理建筑物选择事件
// /// </summary>
// public void OnGeographyBuildingSelected(Building building)
// {
// 	ShowBuildingSelection(building);
// 	buildingCollectionNode?.ShowBuildingVirtual(building);

// 	geographyManager?.ClearAreaRender();

// 	ClearSelectedUnits();
// }


// /// <summary>
// /// 显示建筑选中视觉效果
// /// </summary>
// public void ShowBuildingSelection(Building building)
// {
// 	if (SelectionMesh != null && world?.Ground != null)
// 	{
// 		// 目前建筑统一使用 2x2 选中框
// 		SelectionMesh.ShowBuilding(building.Collision.Center, new Vector2I(1, 1), world.Ground);
// 	}
// }

// /// <summary>
// /// 清除所有选中的单位
// /// </summary>
// public void ClearSelectedUnits()
// {
// 	if (selectedUnits.Count > 0)
// 	{
// 		foreach (var unit in selectedUnits)
// 		{
// 			EmitSignal(SignalName.UnitDeselected, unit);
// 		}
// 		selectedUnits.Clear();
// 		OnUnitUnselected();
// 	}
// }

// /// <summary>
// /// 添加选中的单位
// /// </summary>
// public void AddSelectedUnit(Unit unit)
// {
// 	if (!selectedUnits.Contains(unit))
// 	{
// 		selectedUnits.Add(unit);
// 		EmitSignal(SignalName.UnitSelected, unit);
// 	}
// }

// /// <summary>
// /// 移除选中的单位
// /// </summary>
// public void RemoveSelectedUnit(Unit unit)
// {
// 	if (selectedUnits.Remove(unit))
// 	{
// 		EmitSignal(SignalName.UnitDeselected, unit);
// 		if (selectedUnits.Count == 0)
// 		{
// 			OnUnitUnselected();
// 		}
// 	}
// }