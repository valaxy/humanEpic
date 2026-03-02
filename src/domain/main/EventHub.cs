using Godot;

/// <summary>
/// 全局事件中转站，用于解耦各个系统之间的信号传递。
/// </summary>
[GlobalClass]
public partial class EventHub : RefCounted
{
	private static EventHub? instance;

	/// <summary>
	/// 获取 EventHub 的单例实例。
	/// </summary>
	public static EventHub Instance()
	{
		if (instance == null)
		{
			instance = new EventHub();
		}
		return instance;
	}

	private EventHub() { }

	// // --- Unit 相关的信号 ---

	// /// <summary>
	// /// 当单位人口发生变化时触发。
	// /// </summary>
	// [Signal] public delegate void UnitPopulationChangedEventHandler(Unit unit);

	// /// <summary>
	// /// 当单位计划发生变化时触发。
	// /// </summary>
	// [Signal] public delegate void UnitPlanChangedEventHandler(Unit unit);

	// /// <summary>
	// /// 当单位行为发生变化时触发。
	// /// </summary>
	// [Signal] public delegate void UnitActionChangedEventHandler(Unit unit);

	// /// <summary>
	// /// 当单位行囊（Inventory）发生变化时触发。
	// /// </summary>
	// [Signal] public delegate void UnitInventoryChangedEventHandler(Unit unit);

	// /// <summary>
	// /// 当单位位置发生变化时触发。
	// /// </summary>
	// [Signal] public delegate void UnitPositionChangedEventHandler(Unit unit);

	// /// <summary>
	// /// 当单位路径发生变化时触发。
	// /// </summary>
	// [Signal] public delegate void UnitPathChangedEventHandler(Unit unit);

	// /// <summary>
	// /// 当单位定居点发生变化时触发。
	// /// </summary>
	// [Signal] public delegate void UnitSettlementChangedEventHandler(Unit unit);

	// // --- UnitCollection 相关的信号 ---

	// /// <summary>
	// /// 当战斗开始时触发。
	// /// </summary>
	// [Signal] public delegate void UnitCombatStartedEventHandler(Unit unitA, Unit unitB);

	// /// <summary>
	// /// 当单位死亡时触发。
	// /// </summary>
	// [Signal] public delegate void UnitDeadEventHandler(Unit unit);

	// --- 资源采集相关的信号 ---

	/// <summary>
	/// 当一个地块的资源正在被采集时触发。
	/// </summary>
	[Signal] public delegate void GridGatheringTriggeredEventHandler(Vector2I gridPos);

	/// <summary>
	/// 当地格数据发生变化时触发（例如覆盖物变化、资源耗尽）。
	/// </summary>
	[Signal] public delegate void GroundCellsChangedEventHandler(Godot.Collections.Array<Vector2I> cells);
}
