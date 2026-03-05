using Godot;
using System.Collections.Generic;


/// <summary>
/// 地平面，管理底层的地格数据
/// </summary>
public partial class Ground // IMap
{
	private Grid[,] mapData = new Grid[0, 0];

	/// <summary>
	/// 地图宽度
	/// </summary>
	public int Width => mapData.GetLength(1);

	/// <summary>
	/// 地图高度
	/// </summary>
	public int Height => mapData.GetLength(0);

	/// <summary>
	/// 获取指定坐标的地格数据
	/// </summary>
	/// <param name="x">X 坐标</param>
	/// <param name="y">Y 坐标</param>
	/// <returns>地格对象</returns>
	public Grid GetGrid(int x, int y)
	{
		return mapData[y, x];
	}

	/// <summary>
	/// 判断给定地格坐标是否在地图范围内
	/// </summary>
	public bool IsInsideGround(int x, int y)
	{
		return x >= 0 && x < Width && y >= 0 && y < Height;
	}

	/// <summary>
	/// 判断给定地格坐标是否在地图范围内
	/// </summary>
	public bool IsInsideGround(Vector2I pos)
	{
		return IsInsideGround(pos.X, pos.Y);
	}

	/// <summary>
	/// 调整地图尺寸，保持现有地图居中
	/// </summary>
	/// <param name="newWidth">新宽度</param>
	/// <param name="newHeight">新高度</param>
	public void Resize(int newWidth, int newHeight)
	{
		if (newWidth <= 0 || newHeight <= 0) return;

		Grid[,] newData = new Grid[newHeight, newWidth];
		int oldWidth = Width;
		int oldHeight = Height;

		int offsetX = (newWidth - oldWidth) / 2;
		int offsetY = (newHeight - oldHeight) / 2;

		for (int y = 0; y < newHeight; y++)
		{
			for (int x = 0; x < newWidth; x++)
			{
				int oldX = x - offsetX;
				int oldY = y - offsetY;

				if (oldX >= 0 && oldX < oldWidth && oldY >= 0 && oldY < oldHeight)
				{
					// 处于旧地图范围内，复用地格
					newData[y, x] = mapData[oldY, oldX];
				}
				else
				{
					// 处于扩展区域，寻找最近的地格以继承地表类型
					int clampX = Mathf.Clamp(oldX, 0, oldWidth - 1);
					int clampY = Mathf.Clamp(oldY, 0, oldHeight - 1);

					Grid? nearest = GetGrid(clampX, clampY);
					SurfaceType.Enums inheritedSurface = (nearest != null) ? nearest.SurfaceType : SurfaceType.Enums.GRASSLAND;
					newData[y, x] = new Grid(inheritedSurface, Overlay.None);
				}
			}
		}

		mapData = newData;
	}

	/// <summary>
	/// 统一的地格到世界坐标转换（带高度）
	/// </summary>
	public Vector3 GridToWorld(Vector2I gridPos, float y)
	{
		float x = gridPos.X - Width / 2.0f;
		float z = gridPos.Y - Height / 2.0f;
		return new Vector3(x, y, z);
	}

	/// <summary>
	/// 浮点型的地格坐标转换
	/// </summary>
	public Vector3 GridToWorld(Vector2 gridPos, float y)
	{
		float x = gridPos.X - Width / 2.0f;
		float z = gridPos.Y - Height / 2.0f;
		return new Vector3(x, y, z);
	}

	/// <summary>
	/// 统一的世界坐标到地格坐标转换
	/// </summary>
	public Vector2 WorldToGrid(Vector3 worldPos)
	{
		return new Vector2(
			worldPos.X + Width / 2.0f,
			worldPos.Z + Height / 2.0f
		);
	}

	/// <summary>
	/// 将世界坐标转换为地格坐标。
	/// </summary>
	public Vector2I WorldToCell(Vector3 worldPos)
	{
		Vector2 pos = WorldToGrid(worldPos);
		return new Vector2I(Mathf.FloorToInt(pos.X), Mathf.FloorToInt(pos.Y));
	}

	// /// <summary>
	// /// 设置指定坐标的地格属性
	// /// </summary>
	// /// <param name="x">X 坐标</param>
	// /// <param name="y">Y 坐标</param>
	// /// <param name="surface">地表类型</param>
	// /// <param name="overlayType">覆盖物类型</param>
	// /// <param name="height">地高类型</param>
	// public void SetGridData(int x, int y, SurfaceType.Enums surface, OverlayType.Enums overlayType, TerrainHeight.Enums height)
	// {
	// 	Grid grid = GetGrid(x, y);
	// 	grid.Surface = surface;
	// 	grid.CurrentOverlayType = overlayType;
	// 	grid.Height = height;
	// }

	// /// <summary>
	// /// 仅设置指定坐标的覆盖物类型，不变更地表和地高
	// /// </summary>
	// /// <param name="x">X 坐标</param>
	// /// <param name="y">Y 坐标</param>
	// /// <param name="overlayType">覆盖物类型</param>
	// public void SetGridOverlayData(int x, int y, OverlayType.Enums overlayType)
	// {
	// 	Grid grid = GetGrid(x, y);
	// 	grid.CurrentOverlayType = overlayType;
	// }


	/// <summary>
	/// 获取当前位置周围 8 个方向的可通行邻居
	/// </summary>
	public IEnumerable<Vector2I> GetNeighbors(Vector2I pos)
	{
		// 定义 8 个方向：上下左右 + 4 个对角线方向 (正方形拼接)
		Vector2I[] directions = {
			new(0, 1), new(0, -1), new(1, 0), new(-1, 0),
			new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
		};

		foreach (Vector2I d in directions)
		{
			Vector2I n = pos + d;
			if (IsInsideGround(n))
			{
				if (IsPassable(n))
				{
					yield return n;
				}
			}
		}
	}

	/// <summary>
	/// 启发式估计函数（遵循正方形网格欧几里得距离）
	/// </summary>
	public double Heuristic(Vector2I a, Vector2I b)
	{
		return System.Math.Sqrt(System.Math.Pow(a.X - b.X, 2) + System.Math.Pow(a.Y - b.Y, 2));
	}

	/// <summary>
	/// 获取移动权重：上下左右代价为 1.0，对角线代价约等于 1.414
	/// </summary>
	public double GetMoveCost(Vector2I from, Vector2I to)
	{
		bool isDiagonal = from.X != to.X && from.Y != to.Y;
		return isDiagonal ? System.Math.Sqrt(2.0) : 1.0;
	}

	/// <summary>
	/// 检查格点是否通行（目前规则为中空覆盖物即可通过）
	/// </summary>
	public bool IsPassable(Vector2I pos)
	{
		return GetGrid(pos.X, pos.Y).SurfaceType != SurfaceType.Enums.RIVER;
	}
}
