using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// 领域层统一模拟入口，管理并更新所有的世界逻辑（IWorldLogic）
/// </summary>
public class Simulation
{
	/// <summary>
	/// 当任意世界逻辑触发时发出。
	/// </summary>
	public event Action<IWorldLogic>? LogicTriggered;

	// 当前已注册的世界逻辑集合。
	private readonly List<IWorldLogic> logics = new();

	/// <summary>
	/// 构造函数，初始化并注册可用世界逻辑。
	/// </summary>
	public Simulation(GameWorld world)
	{
		logics.Add(new HelloWorldLogic());
		logics.Add(new ConsumptionPurchaseLogic(world.Populations, world.Buildings));
		logics.ForEach(logic => logic.Triggered += onLogicTriggered);
	}

	/// <summary>
	/// 每帧更新所有已注册的逻辑
	/// </summary>
	/// <param name="delta">时间增量（秒）</param>
	public void Update(double delta)
	{
		logics.ForEach(logic => logic.UpdateDelta((float)delta));
	}

	/// <summary>
	/// 获取所有世界逻辑（供 UI 展示名称和进度）
	/// </summary>
	public IReadOnlyList<IWorldLogic> GetWorldLogics()
	{
		return logics;
	}

	// 转发逻辑触发事件。
	private void onLogicTriggered(IWorldLogic logic)
	{
		LogicTriggered?.Invoke(logic);
	}
}
