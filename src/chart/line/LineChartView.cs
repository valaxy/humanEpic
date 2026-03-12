using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 折线图组件，使用 DataSource 中的序列数据进行绘制。
/// </summary>
[GlobalClass]
public partial class LineChartView : Control
{
    // 图表左侧内边距（为 Y 轴刻度留白）。
    private const float PlotPaddingLeft = 72f;

    // 图表右侧内边距。
    private const float PlotPaddingRight = 18f;

    // 图表上侧内边距。
    private const float PlotPaddingTop = 12f;

    // 图表下侧内边距。
    private const float PlotPaddingBottom = 30f;

    // Y 轴标签与轴线间距。
    private const float YLabelGap = 8f;

    // Y 轴标签可用宽度。
    private const float YLabelWidth = 56f;

    // 标题区域高度。
    private const float TitleHeight = 24f;

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

    // 悬浮点半径。
    private const float PointRadius = 3.5f;

    // 悬浮命中半径。
    private const float HoverRadius = 9f;

    // 当前图表配置。
    private Chart chart = Chart.Create(
        Axis.Create("X"),
        Axis.Create("Y"),
        DataSource.CreateLineChart(string.Empty, Array.Empty<string>(), Array.Empty<DataSeries>()));

    // 当前可交互点集合。
    private List<InteractivePoint> interactivePoints = [];

    // 当前可交互图例集合。
    private List<InteractiveLegendItem> interactiveLegendItems = [];

    // 图例可见性状态（Key -> 是否可见）。
    private Dictionary<string, bool> legendVisibility = new();

    // 当前悬浮图例键。
    private string? hoveredLegendKey;

    // 当前悬浮点。
    private InteractivePoint? hoveredPoint;

    // 悬浮提示面板。
    private PanelContainer tooltipPanel = null!;

    // 悬浮提示文本。
    private Label tooltipLabel = null!;

    private sealed record InteractivePoint(string SeriesKey, Vector2 Position, string XText, string YText, Color Color);

    private sealed record InteractiveLegendItem(string Key, Rect2 ToggleRect, Rect2 HoverRect);

    /// <summary>
    /// 初始化交互节点。
    /// </summary>
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        tooltipLabel = new Label();
        tooltipPanel = new PanelContainer
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 100,
            Position = Vector2.Zero
        };
        tooltipPanel.AddChild(tooltipLabel);
        AddChild(tooltipPanel);
        MouseExited += hideTooltip;
    }

    /// <summary>
    /// 设置并渲染折线图数据源。
    /// </summary>
    /// <param name="dataSource">图表数据源。</param>
    public void Render(DataSource dataSource)
    {
        chart = chart.Update(dataSource: dataSource);
        syncLegendState(dataSource);
        QueueRedraw();
    }

    /// <summary>
    /// 设置并刷新图表配置。
    /// </summary>
    /// <param name="chart">图表配置对象。</param>
    public void UpdateChart(Chart chart)
    {
        this.chart = chart;
        syncLegendState(chart.DataSource);
        QueueRedraw();
    }

    /// <summary>
    /// 绘制控件内容。
    /// </summary>
    public override void _Draw()
    {
        interactivePoints = [];
        interactiveLegendItems = [];

        Color labelColor = GetThemeColor("font_color", "Label");
        Font? font = GetThemeDefaultFont();
        int fontSize = GetThemeDefaultFontSize();
        DataSource dataSource = chart.DataSource;
        syncLegendState(dataSource);

        if (!string.IsNullOrEmpty(dataSource.Title) && font is not null)
        {
            DrawString(font, new Vector2(0, TitleHeight - 6f), dataSource.Title, HorizontalAlignment.Left, -1, fontSize, labelColor);
        }

        bool hasLegend = getLegendEntries(dataSource).Count > 0;
        float legendAreaWidth = hasLegend ? LegendWidth + LegendGap : 0f;

        Rect2 plotRect = new Rect2(
            PlotPaddingLeft,
            TitleHeight + PlotPaddingTop,
            Size.X - PlotPaddingLeft - PlotPaddingRight - legendAreaWidth,
            Size.Y - TitleHeight - PlotPaddingTop - PlotPaddingBottom);

        if (plotRect.Size.X <= 0 || plotRect.Size.Y <= 0)
        {
            return;
        }

        DrawRect(plotRect, labelColor, false, 1f);

        PlotBounds? bounds = tryBuildBounds();
        if (bounds is null)
        {
            return;
        }

        drawSeries(plotRect, bounds.Value);
        if (font != null)
        {
            drawYTicks(plotRect, bounds.Value, font, fontSize, labelColor);
            drawXLabels(plotRect, bounds.Value, font, fontSize, labelColor);
            if (hasLegend)
            {
                drawLegend(plotRect, font, fontSize, labelColor);
            }
        }

        if (hoveredPoint is not null)
        {
            drawHoveredPoint(hoveredPoint);
        }
    }

    /// <summary>
    /// 鼠标交互处理，驱动关键点悬浮提示。
    /// </summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            updateHoveredLegend(mouseMotion.Position);
            updateHoveredPoint(mouseMotion.Position);
            return;
        }

        if (@event is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex == MouseButton.Left)
        {
            handleLegendToggle(mouseButton.Position);
            return;
        }
    }

    // 绘制所有折线序列。
    private void drawSeries(Rect2 plotRect, PlotBounds bounds)
    {
        DataSource dataSource = chart.DataSource;
        List<DataSeries> seriesList = getVisibleSeries(dataSource);
        if (seriesList.Count == 0)
        {
            return;
        }

        List<InteractivePoint> points = [];

        seriesList
            .Where(series => series.Values.Count > 0)
            .ToList()
            .ForEach(series =>
            {
                Color seriesColor = toGodotColor(series.ColorHex);
                string seriesKey = getSeriesKey(series);
                bool highlightedByLegend = string.IsNullOrWhiteSpace(hoveredLegendKey) || hoveredLegendKey == seriesKey;
                Color renderColor = highlightedByLegend ? seriesColor : seriesColor.Darkened(0.45f);
                float lineWidth = highlightedByLegend ? 2.6f : 1.5f;
                List<PointState> pointStates = Enumerable.Range(0, series.Values.Count)
                    .Select(index => createPointState(series, index, plotRect, bounds))
                    .ToList();

                if (pointStates.Count > 1)
                {
                    Enumerable.Range(0, pointStates.Count - 1)
                        .Where(index => pointStates[index].Visible && pointStates[index + 1].Visible)
                        .ToList()
                        .ForEach(index => DrawLine(pointStates[index].Position, pointStates[index + 1].Position, renderColor, lineWidth, true));
                }

                pointStates
                    .Where(state => state.Visible)
                    .ToList()
                    .ForEach(state =>
                    {
                        DrawCircle(state.Position, highlightedByLegend ? PointRadius : PointRadius - 0.8f, renderColor);
                        points.Add(new InteractivePoint(seriesKey, state.Position, state.XText, state.YText, seriesColor));
                    });
            });

        interactivePoints = points;
    }

    // 绘制 Y 轴刻度。
    private void drawYTicks(Rect2 plotRect, PlotBounds bounds, Font font, int fontSize, Color color)
    {
        List<float> yTicks = chart.YAxis.GenerateTicks(bounds.YMin, bounds.YMax)
            .Distinct()
            .Where(tick => tick >= bounds.YMin && tick <= bounds.YMax)
            .OrderBy(tick => tick)
            .ToList();

        if (yTicks.Count == 0)
        {
            yTicks = [bounds.YMin, bounds.YMax];
        }

        yTicks.ForEach(tick =>
        {
            float y = mapY(plotRect, bounds, tick);
            DrawLine(new Vector2(plotRect.Position.X - 4f, y), new Vector2(plotRect.Position.X, y), color, 1f, true);
            float labelX = plotRect.Position.X - YLabelGap - YLabelWidth;
            DrawString(font, new Vector2(labelX, y + fontSize / 3f), chart.YAxis.Format(tick), HorizontalAlignment.Right, YLabelWidth, fontSize - 1, color);
        });
    }

    // 绘制 X 轴标签。
    private void drawXLabels(Rect2 plotRect, PlotBounds bounds, Font font, int fontSize, Color color)
    {
        DataSource dataSource = chart.DataSource;
        List<LineAxisPoint> axisPoints = dataSource.AxisPoints.ToList();
        int maxPointCount = axisPoints.Count;

        if (maxPointCount == 0)
        {
            return;
        }

        List<int> visibleIndices = Enumerable.Range(0, maxPointCount)
            .Where(index => chart.XAxis.IsInRange(getXValue(dataSource, index)))
            .ToList();

        if (visibleIndices.Count == 0)
        {
            return;
        }

        int stride = Math.Max(1, (int)MathF.Ceiling(visibleIndices.Count / 8f));
        int lastVisibleIndex = visibleIndices[^1];

        visibleIndices
            .Where((index, offset) => offset % stride == 0 || index == lastVisibleIndex)
            .Distinct()
            .ToList()
            .ForEach(index =>
            {
                float xValue = getXValue(dataSource, index);
                float x = mapX(plotRect, bounds, xValue);
                string xText = getXText(dataSource, index);

                DrawLine(new Vector2(x, plotRect.End.Y), new Vector2(x, plotRect.End.Y + 4f), color, 1f, true);
                DrawString(font, new Vector2(x, plotRect.End.Y + fontSize + 2f), xText, HorizontalAlignment.Center, 120f, fontSize - 1, color);
            });
    }

    // 创建一个点状态。
    private PointState createPointState(DataSeries series, int index, Rect2 plotRect, PlotBounds bounds)
    {
        DataSource dataSource = chart.DataSource;
        float xValue = getXValue(dataSource, index);
        float yValue = series.Values[index];
        bool visible = !float.IsNaN(yValue) && isPointVisible(xValue, yValue, bounds);
        Vector2 position = new Vector2(mapX(plotRect, bounds, xValue), mapY(plotRect, bounds, yValue));
        string xText = getXText(dataSource, index);
        string yText = chart.YAxis.Format(yValue);

        return new PointState(position, xText, yText, visible);
    }

    // 根据鼠标位置更新当前悬浮点。
    private void updateHoveredPoint(Vector2 mousePosition)
    {
        InteractivePoint? nearestPoint = interactivePoints
            .Where(point => string.IsNullOrWhiteSpace(hoveredLegendKey) || point.SeriesKey == hoveredLegendKey)
            .Select(point => new { Point = point, Distance = point.Position.DistanceTo(mousePosition) })
            .Where(candidate => candidate.Distance <= HoverRadius)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => (InteractivePoint?)candidate.Point)
            .FirstOrDefault();

        hoveredPoint = nearestPoint;
        if (nearestPoint is null)
        {
            hideTooltip();
            QueueRedraw();
            return;
        }

        showTooltip(nearestPoint, mousePosition);
        QueueRedraw();
    }

    // 绘制图例区域。
    private void drawLegend(Rect2 plotRect, Font font, int fontSize, Color color)
    {
        DataSource dataSource = chart.DataSource;
        List<LineLegendItem> legendEntries = getLegendEntries(dataSource);
        if (legendEntries.Count == 0)
        {
            return;
        }

        float legendX = plotRect.End.X + LegendGap;
        float legendY = plotRect.Position.Y;
        float legendHeight = LegendPadding * 2f + legendEntries.Count * LegendItemHeight;
        Rect2 legendRect = new Rect2(legendX, legendY, LegendWidth, legendHeight);
        DrawRect(legendRect, color.Darkened(0.7f), false, 1f);

        legendEntries
            .Select((entry, index) => (entry, index))
            .ToList()
            .ForEach(item =>
            {
                float rowY = legendRect.Position.Y + LegendPadding + item.index * LegendItemHeight;
                Rect2 rowRect = new Rect2(legendRect.Position.X + 2f, rowY, legendRect.Size.X - 4f, LegendItemHeight);
                Rect2 toggleRect = new Rect2(rowRect.Position.X + 4f, rowRect.Position.Y + (LegendItemHeight - LegendToggleSize) / 2f, LegendToggleSize, LegendToggleSize);

                bool isVisible = legendVisibility.TryGetValue(item.entry.Key, out bool visible) && visible;
                bool isHovered = hoveredLegendKey == item.entry.Key;

                if (isHovered)
                {
                    DrawRect(rowRect, color.Darkened(0.65f), true);
                }

                DrawRect(toggleRect, color, false, 1f);
                if (isVisible)
                {
                    Rect2 innerToggleRect = new Rect2(toggleRect.Position + new Vector2(2f, 2f), toggleRect.Size - new Vector2(4f, 4f));
                    DrawRect(innerToggleRect, toGodotColor(item.entry.ColorHex), true);
                }

                Vector2 colorDotPosition = new Vector2(toggleRect.End.X + 10f, rowRect.Position.Y + LegendItemHeight / 2f);
                DrawCircle(colorDotPosition, 4f, toGodotColor(item.entry.ColorHex));

                float textStartX = colorDotPosition.X + 10f;
                DrawString(font, new Vector2(textStartX, rowRect.Position.Y + LegendItemHeight * 0.72f), item.entry.Name, HorizontalAlignment.Left, legendRect.Size.X - (textStartX - legendRect.Position.X) - 4f, fontSize - 1, color);

                interactiveLegendItems.Add(new InteractiveLegendItem(item.entry.Key, toggleRect, rowRect));
            });
    }

    // 更新图例悬浮项。
    private void updateHoveredLegend(Vector2 mousePosition)
    {
        InteractiveLegendItem? legendItem = interactiveLegendItems
            .Select(item => (InteractiveLegendItem?)item)
            .FirstOrDefault(item => item != null && item.HoverRect.HasPoint(mousePosition));

        string? nextHoveredLegendKey = legendItem?.Key;
        if (nextHoveredLegendKey == hoveredLegendKey)
        {
            return;
        }

        hoveredLegendKey = nextHoveredLegendKey;
        QueueRedraw();
    }

    // 处理图例勾选切换。
    private void handleLegendToggle(Vector2 mousePosition)
    {
        InteractiveLegendItem? legendItem = interactiveLegendItems
            .Select(item => (InteractiveLegendItem?)item)
            .FirstOrDefault(item => item != null && item.ToggleRect.HasPoint(mousePosition));
        if (legendItem == null)
        {
            return;
        }

        bool currentVisible = legendVisibility.TryGetValue(legendItem.Key, out bool value) && value;
        legendVisibility[legendItem.Key] = !currentVisible;

        if (hoveredPoint != null && hoveredPoint.SeriesKey == legendItem.Key && !legendVisibility[legendItem.Key])
        {
            hideTooltip();
        }

        QueueRedraw();
    }

    // 显示悬浮提示。
    private void showTooltip(InteractivePoint point, Vector2 mousePosition)
    {
        tooltipLabel.Text = $"X: {point.XText}\nY: {point.YText}";
        tooltipPanel.Visible = true;

        Vector2 tooltipSize = tooltipPanel.GetCombinedMinimumSize();
        float x = Mathf.Clamp(mousePosition.X + 12f, 0f, Math.Max(0f, Size.X - tooltipSize.X));
        float y = Mathf.Clamp(mousePosition.Y - tooltipSize.Y - 10f, 0f, Math.Max(0f, Size.Y - tooltipSize.Y));
        tooltipPanel.Position = new Vector2(x, y);
    }

    // 隐藏悬浮提示。
    private void hideTooltip()
    {
        tooltipPanel.Visible = false;
        hoveredPoint = null;
    }

    // 绘制当前悬浮点高亮。
    private void drawHoveredPoint(InteractivePoint point)
    {
        DrawCircle(point.Position, PointRadius + 2f, point.Color.Lightened(0.2f));
        DrawCircle(point.Position, PointRadius, point.Color);
    }

    // 构建绘图边界。
    private PlotBounds? tryBuildBounds()
    {
        DataSource dataSource = chart.DataSource;
        List<DataSeries> seriesList = getVisibleSeries(dataSource);
        if (seriesList.Count == 0)
        {
            return null;
        }

        int maxPointCount = dataSource.AxisPoints.Count;
        if (maxPointCount == 0)
        {
            return null;
        }

        List<float> visibleXValues = Enumerable.Range(0, maxPointCount)
            .Select(index => getXValue(dataSource, index))
            .Where(value => chart.XAxis.IsInRange(value))
            .ToList();

        if (visibleXValues.Count == 0)
        {
            return null;
        }

        List<float> visibleYValues = seriesList
            .SelectMany(series => Enumerable.Range(0, series.Values.Count)
                .Where(index => chart.XAxis.IsInRange(getXValue(dataSource, index)))
                .Select(index => series.Values[index])
                .Where(value => !float.IsNaN(value))
                .Where(value => chart.YAxis.IsInRange(value)))
            .ToList();

        if (visibleYValues.Count == 0)
        {
            return null;
        }

        float xMin = chart.XAxis.MinTick ?? visibleXValues.Min();
        float xMax = chart.XAxis.MaxTick ?? visibleXValues.Max();
        float yMin = chart.YAxis.MinTick ?? visibleYValues.Min();
        float yMax = chart.YAxis.MaxTick ?? visibleYValues.Max();

        if (Mathf.IsEqualApprox(xMin, xMax))
        {
            xMin -= 1f;
            xMax += 1f;
        }

        if (Mathf.IsEqualApprox(yMin, yMax))
        {
            yMin -= 1f;
            yMax += 1f;
        }

        return new PlotBounds(xMin, xMax, yMin, yMax);
    }

    // 判断点是否可见。
    private bool isPointVisible(float xValue, float yValue, PlotBounds bounds)
    {
        bool inTickRange = chart.XAxis.IsInRange(xValue) && chart.YAxis.IsInRange(yValue);
        bool inDisplayRange = xValue >= bounds.XMin && xValue <= bounds.XMax && yValue >= bounds.YMin && yValue <= bounds.YMax;
        return inTickRange && inDisplayRange;
    }

    // 获取 X 值。
    private static float getXValue(DataSource dataSource, int index)
    {
        if (index >= 0 && index < dataSource.AxisPoints.Count)
        {
            return dataSource.AxisPoints[index].Value;
        }

        return index;
    }

    // 获取 X 轴显示文本。
    private string getXText(DataSource dataSource, int index)
    {
        if (index >= 0 && index < dataSource.AxisPoints.Count)
        {
            return dataSource.AxisPoints[index].Label;
        }

        return chart.XAxis.Format(index);
    }

    // 同步图例状态，保留已有勾选结果。
    private void syncLegendState(DataSource dataSource)
    {
        List<LineLegendItem> legendEntries = getLegendEntries(dataSource);
        if (legendEntries.Count == 0)
        {
            legendVisibility.Clear();
            hoveredLegendKey = null;
            return;
        }

        Dictionary<string, bool> nextVisibility = legendEntries
            .ToDictionary(
                entry => entry.Key,
                entry => legendVisibility.TryGetValue(entry.Key, out bool visible) ? visible : true);
        legendVisibility = nextVisibility;

        if (!string.IsNullOrWhiteSpace(hoveredLegendKey) && !legendVisibility.ContainsKey(hoveredLegendKey))
        {
            hoveredLegendKey = null;
        }
    }

    // 获取有效图例项（优先使用数据源显式图例定义）。
    private static List<LineLegendItem> getLegendEntries(DataSource dataSource)
    {
        if (dataSource.LegendItems.Count > 0)
        {
            return dataSource.LegendItems.ToList();
        }

        return dataSource.SeriesList
            .Select(series => new LineLegendItem(getSeriesKey(series), series.Name, series.ColorHex))
            .ToList();
    }

    // 获取当前可见序列。
    private List<DataSeries> getVisibleSeries(DataSource dataSource)
    {
        List<LineLegendItem> legendEntries = getLegendEntries(dataSource);
        HashSet<string> enabledKeys = legendEntries
            .Where(entry => legendVisibility.TryGetValue(entry.Key, out bool visible) && visible)
            .Select(entry => entry.Key)
            .ToHashSet();

        if (legendEntries.Count == 0)
        {
            return dataSource.SeriesList.ToList();
        }

        return dataSource.SeriesList
            .Where(series => enabledKeys.Contains(getSeriesKey(series)))
            .ToList();
    }

    private static string getSeriesKey(DataSeries series)
    {
        return string.IsNullOrWhiteSpace(series.Key)
            ? series.Name
            : series.Key;
    }

    // 将 X 值映射到图表坐标。
    private static float mapX(Rect2 plotRect, PlotBounds bounds, float xValue)
    {
        float ratio = (xValue - bounds.XMin) / bounds.XRange;
        return plotRect.Position.X + ratio * plotRect.Size.X;
    }

    // 将 Y 值映射到图表坐标。
    private static float mapY(Rect2 plotRect, PlotBounds bounds, float yValue)
    {
        float ratio = (yValue - bounds.YMin) / bounds.YRange;
        return plotRect.End.Y - ratio * plotRect.Size.Y;
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

    private readonly record struct PlotBounds(float XMin, float XMax, float YMin, float YMax)
    {
        public float XRange => XMax - XMin;
        public float YRange => YMax - YMin;
    }

    private readonly record struct PointState(Vector2 Position, string XText, string YText, bool Visible);
}