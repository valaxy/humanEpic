using Godot;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// 采集流程模型，负责覆盖层采集与产物入库。
/// </summary>
public class Harvest : IInfo
{
	/// <summary>
	/// 采集目标覆盖物类型。
	/// </summary>
	public OverlayType.Enums TargetOverlayType { get; }

	/// <summary>
	/// 采集范围。
	/// </summary>
	public CircularCoverage CollectCoverage { get; }

	/// <summary>
	/// 初始化采集流程。
	/// </summary>
	public Harvest(OverlayType.Enums targetOverlayType, float collectionRadius)
	{
		TargetOverlayType = targetOverlayType;
		CollectCoverage = new CircularCoverage(collectionRadius);
	}

	/// <summary>
	/// 获取用于 UI 展示的采集信息。
	/// </summary>
	public InfoData GetInfoData()
	{
		InfoData data = new InfoData();
		string overlayName = OverlayTemplate.GetTemplate(TargetOverlayType).Name;
		data.AddText("采集目标", overlayName);
		data.AddNumber("采集半径", CollectCoverage.Radius);
		return data;
	}


	// /// <summary>
	// /// 执行一次采集，返回实际采集量。
	// /// </summary>
	// /// <param name="changedCells">记录地格变化，用于地表刷新等。</param>
	// public float HarvestOverlay(Building building, Ground ground, float deltaDays, GCol.Array<Vector2I> changedCells)
	// {
	// 	// 仓库已满则无法继续采集。
	// 	if (building.Warehouse.RemainingVolume <= 0.0f)
	// 	{
	// 		return 0.0f;
	// 	}

	// 	// 计算采集范围的圆心与半径（注意将建筑中心转为格子中心坐标）。
	// 	float radius = CollectionRange.Radius;
	// 	float centerX = building.Collision.Center.X + 0.5f;
	// 	float centerY = building.Collision.Center.Y + 0.5f;
	// 	List<Vector2I> cells = CircularGridTool.GetCellsByCenter(
	// 		new Vector2(centerX, centerY),
	// 		radius,
	// 		ground.Width,
	// 		ground.Height
	// 	);

	// 	// 过滤出可采集且有剩余产物的格子索引。
	// 	List<int> targetIndices = new List<int>();
	// 	for (int i = 0; i < cells.Count; i++)
	// 	{
	// 		Vector2I pos = cells[i];
	// 		Grid grid = ground.GetGrid(pos.X, pos.Y);
	// 		if (grid.CurrentOverlayType == TargetOverlayType && grid.OverlayInstance != null && grid.OverlayInstance.Amount > 0)
	// 		{
	// 			targetIndices.Add(i);
	// 		}
	// 	}

	// 	// 若无可采集格子则结束。
	// 	if (targetIndices.Count == 0)
	// 	{
	// 		return 0.0f;
	// 	}

	// 	// 需要有工人才能采集。
	// 	int workerCount = building.Workforce.CurrentCount;
	// 	if (workerCount <= 0)
	// 	{
	// 		return 0.0f;
	// 	}

	// 	// 计算本次采集的总量（基于每人每日采集量、工人数量与时间增量），并平均分配到每个目标格子。
	// 	float collectAmount = CollectionSpeedPerDay * workerCount * deltaDays; // TODO 这里的生产力要调整？
	// 	float amountPerTarget = collectAmount / targetIndices.Count;

	// 	// 对每个目标格子进行扣减，累计实际采集到的数量。
	// 	float actualCollected = 0.0f;
	// 	foreach (int idx in targetIndices)
	// 	{
	// 		Vector2I pos = cells[idx];
	// 		Grid grid = ground.GetGrid(pos.X, pos.Y);
	// 		Overlay overlay = grid.OverlayInstance;

	// 		// 从格子上采集，不超过该格剩余数量或分配到该格的采集量。
	// 		float toTake = Mathf.Min(overlay.Amount, amountPerTarget);
	// 		overlay.Amount -= toTake;
	// 		actualCollected += toTake;

	// 		// 若格子资源被采空，则清除覆盖物类型并记录变化格子（用于地表刷新等）。
	// 		if (overlay.Amount <= 0)
	// 		{
	// 			grid.CurrentOverlayType = OverlayType.Enums.NONE;
	// 			if (changedCells != null)
	// 			{
	// 				changedCells.Add(pos);
	// 			}
	// 		}
	// 	}

	// 	return actualCollected;
	// }
}