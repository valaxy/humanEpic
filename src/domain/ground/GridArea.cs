using Godot;
using System.Collections.Generic;
using GCol = Godot.Collections;

/// <summary>
/// 地格区域逻辑，负责管理和搜索连通的地格区域
/// </summary>
[GlobalClass]
public partial class GridArea : RefCounted
{
	/// <summary>
	/// 搜寻与起始地格联通的所有同类覆盖物地格
	/// </summary>
	/// <param name="startPos">起始地格坐标</param>
	/// <param name="ground">地型数据</param>
	/// <returns>连通地格坐标数组</returns>
	public static GCol.Array<Vector2I> FindConnectedOverlayArea(Vector2I startPos, Ground ground)
	{
		Grid grid = ground.GetGrid(startPos.X, startPos.Y);
		if (grid.OverlayType == OverlayType.Enums.NONE)
		{
			return new GCol.Array<Vector2I>();

		}

		OverlayType.Enums targetOverlay = grid.OverlayType;
		GCol.Array<Vector2I> area = new GCol.Array<Vector2I>();
		HashSet<Vector2I> visited = new();
		Queue<Vector2I> queue = new();

		queue.Enqueue(startPos);
		visited.Add(startPos);

		while (queue.Count > 0)
		{
			Vector2I current = queue.Dequeue();
			area.Add(current);

			// 检查相邻的四个方向
			Vector2I[] neighbors = new Vector2I[]
			{
				new(current.X + 1, current.Y),
				new(current.X - 1, current.Y),
				new(current.X, current.Y + 1),
				new(current.X, current.Y - 1)
			};

			foreach (Vector2I neighbor in neighbors)
			{
				if (neighbor.X < 0 || neighbor.X >= ground.Width || neighbor.Y < 0 || neighbor.Y >= ground.Height)
				{
					continue;
				}

				if (!visited.Contains(neighbor))
				{
					Grid neighborGrid = ground.GetGrid(neighbor.X, neighbor.Y);
					if (neighborGrid.OverlayType == targetOverlay)
					{
						visited.Add(neighbor);
						queue.Enqueue(neighbor);
					}
				}
			}
		}

		return area;
	}
}
