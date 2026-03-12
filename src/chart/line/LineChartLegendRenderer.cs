using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 折线图图例渲染与交互命中处理器。
/// </summary>
public sealed class LineChartLegendRenderer
{
	// 图例区域宽度。
	private const float LegendWidth = 180f;

	// 图例区域与绘图区间距。
	private const float LegendGap = 12f;

	// 图例行高。
	private const float LegendItemHeight = 22f;

	// 图例内边距。
	private const float LegendPadding = 8f;

	// 图例勾选框尺寸。
	private const float LegendToggleSize = 12f;

	/// <summary>
	/// 图例占用宽度（含间距）。
	/// </summary>
	public float LegendAreaWidth => LegendWidth + LegendGap;

	/// <summary>
	/// 判断数据源是否包含图例。
	/// </summary>
	public bool HasLegend(Chart chart)
	{
		return GetLegendEntries(chart).Count > 0;
	}

	/// <summary>
	/// 同步图例可见性状态，保留已存在的勾选结果。
	/// </summary>
	public void SyncLegendState(Chart chart, ref Dictionary<string, bool> legendVisibility, ref string? hoveredLegendKey)
	{
		List<LineLegendItem> legendEntries = GetLegendEntries(chart);
		if (legendEntries.Count == 0)
		{
			legendVisibility.Clear();
			hoveredLegendKey = null;
			return;
		}

		Dictionary<string, bool> previousVisibility = legendVisibility;

		legendVisibility = legendEntries
			.ToDictionary(
				entry => entry.Key,
				entry => previousVisibility.TryGetValue(entry.Key, out bool visible) ? visible : true);

		if (!string.IsNullOrWhiteSpace(hoveredLegendKey) && !legendVisibility.ContainsKey(hoveredLegendKey))
		{
			hoveredLegendKey = null;
		}
	}

	/// <summary>
	/// 绘制图例并返回交互命中区域。
	/// </summary>
	public List<LineChartLegendInteractiveItem> DrawLegend(
		Control owner,
		Rect2 plotRect,
		Font font,
		int fontSize,
		Color color,
		Chart chart,
		IReadOnlyDictionary<string, bool> legendVisibility,
		string? hoveredLegendKey)
	{
		List<LineLegendItem> legendEntries = GetLegendEntries(chart);
		if (legendEntries.Count == 0)
		{
			return [];
		}

		float legendX = plotRect.End.X + LegendGap;
		float legendY = plotRect.Position.Y;
		float legendHeight = LegendPadding * 2f + legendEntries.Count * LegendItemHeight;
		Rect2 legendRect = new Rect2(legendX, legendY, LegendWidth, legendHeight);
		owner.DrawRect(legendRect, color.Darkened(0.7f), false, 1f);

		return legendEntries
			.Select((entry, index) => (entry, index))
			.Select(item =>
			{
				float rowY = legendRect.Position.Y + LegendPadding + item.index * LegendItemHeight;
				Rect2 rowRect = new Rect2(legendRect.Position.X + 2f, rowY, legendRect.Size.X - 4f, LegendItemHeight);
				Rect2 toggleRect = new Rect2(rowRect.Position.X + 4f, rowRect.Position.Y + (LegendItemHeight - LegendToggleSize) / 2f, LegendToggleSize, LegendToggleSize);

				bool isVisible = legendVisibility.TryGetValue(item.entry.Key, out bool visible) && visible;
				bool isHovered = hoveredLegendKey == item.entry.Key;

				if (isHovered)
				{
					owner.DrawRect(rowRect, color.Darkened(0.65f), true);
				}

				owner.DrawRect(toggleRect, color, false, 1f);
				if (isVisible)
				{
					Rect2 innerToggleRect = new Rect2(toggleRect.Position + new Vector2(2f, 2f), toggleRect.Size - new Vector2(4f, 4f));
					owner.DrawRect(innerToggleRect, toGodotColor(item.entry.ColorHex), true);
				}

				Vector2 colorDotPosition = new Vector2(toggleRect.End.X + 10f, rowRect.Position.Y + LegendItemHeight / 2f);
				owner.DrawCircle(colorDotPosition, 4f, toGodotColor(item.entry.ColorHex));

				float textStartX = colorDotPosition.X + 10f;
				owner.DrawString(
					font,
					new Vector2(textStartX, rowRect.Position.Y + LegendItemHeight * 0.72f),
					item.entry.Name,
					HorizontalAlignment.Left,
					legendRect.Size.X - (textStartX - legendRect.Position.X) - 4f,
					fontSize - 1,
					color);

				return new LineChartLegendInteractiveItem(item.entry.Key, toggleRect, rowRect);
			})
			.ToList();
	}

	/// <summary>
	/// 命中图例悬浮区域。
	/// </summary>
	public string? ResolveHoveredLegendKey(IEnumerable<LineChartLegendInteractiveItem> items, Vector2 mousePosition)
	{
		LineChartLegendInteractiveItem? legendItem = items
			.Select(item => (LineChartLegendInteractiveItem?)item)
			.FirstOrDefault(item => item != null && item.HoverRect.HasPoint(mousePosition));
		return legendItem?.Key;
	}

	/// <summary>
	/// 命中图例勾选框区域。
	/// </summary>
	public string? ResolveToggleLegendKey(IEnumerable<LineChartLegendInteractiveItem> items, Vector2 mousePosition)
	{
		LineChartLegendInteractiveItem? legendItem = items
			.Select(item => (LineChartLegendInteractiveItem?)item)
			.FirstOrDefault(item => item != null && item.ToggleRect.HasPoint(mousePosition));
		return legendItem?.Key;
	}

	/// <summary>
	/// 计算当前可见序列。
	/// </summary>
	public List<DataSeries> GetVisibleSeries(Chart chart, IReadOnlyDictionary<string, bool> legendVisibility)
	{
		List<LineLegendItem> legendEntries = GetLegendEntries(chart);
		if (legendEntries.Count == 0)
		{
			return chart.SeriesList.ToList();
		}

		HashSet<string> enabledKeys = legendEntries
			.Where(entry => legendVisibility.TryGetValue(entry.Key, out bool visible) && visible)
			.Select(entry => entry.Key)
			.ToHashSet();

		return chart.SeriesList
			.Where(series => enabledKeys.Contains(LineChartDataSourceFactory.ResolveSeriesKey(series)))
			.ToList();
	}

	// 获取有效图例项（优先使用数据源显式图例定义）。
	private static List<LineLegendItem> GetLegendEntries(Chart chart)
	{
		if (chart.LegendItems.Count > 0)
		{
			return chart.LegendItems.ToList();
		}

		return chart.SeriesList
			.Select(series => new LineLegendItem(LineChartDataSourceFactory.ResolveSeriesKey(series), series.Name, series.ColorHex))
			.ToList();
	}

	// 将十六进制颜色字符串转换为 Godot Color。
	private static Color toGodotColor(string colorHex)
	{
		if (string.IsNullOrWhiteSpace(colorHex))
		{
			return Colors.White;
		}

		return Color.FromHtml(colorHex);
	}
}

/// <summary>
/// 图例可交互区域。
/// </summary>
public sealed record LineChartLegendInteractiveItem(string Key, Rect2 ToggleRect, Rect2 HoverRect);
