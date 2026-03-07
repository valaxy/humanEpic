using System;
using System.Collections.Generic;
using Vector2I = Godot.Vector2I;
using Vector2 = Godot.Vector2;

/// <summary>
/// 通用的圆形范围类，用于表示圆形的影响范围
/// </summary>
public class CircularCoverage
{
	/// <summary>
	/// 范围半径
	/// </summary>
	public float Radius { get; }

	public CircularCoverage(float radius)
	{
		Radius = radius;
	}

	/// <summary>
	/// 以center（任意浮点数坐标）为中心，半径为 Radius 的圆形范围内的所有格子坐标
	/// 只有格子的中点位于圆形范围内才算覆盖到，若中心位于边界点上也包含
	/// </summary>
	public List<Vector2I> GetCoveredCells(Vector2 center, int groundWidth, int groundHeight)
	{
		List<Vector2I> cells = new List<Vector2I>();

		int minWidth = 0, minHeight = 0;
		int maxWidth = groundWidth - 1;
		int maxHeight = groundHeight - 1;

		int startX = Math.Max(minWidth, (int)Math.Floor(center.X - Radius));
		int endX = Math.Min(maxWidth, (int)Math.Ceiling(center.X + Radius));
		int startY = Math.Max(minHeight, (int)Math.Floor(center.Y - Radius));
		int endY = Math.Min(maxHeight, (int)Math.Ceiling(center.Y + Radius));

		float radiusSq = Radius * Radius;
		for (int y = startY; y <= endY; y++)
		{
			for (int x = startX; x <= endX; x++)
			{
				// 按格子中点 (x+0.5, y+0.5) 计算距离，边界点包含在内
				float dx = center.X - (x + 0.5f);
				float dy = center.Y - (y + 0.5f);
				float distSq = dx * dx + dy * dy;
				if (distSq <= radiusSq)
				{
					cells.Add(new Vector2I(x, y));
				}
			}
		}

		return cells;
	}
}
