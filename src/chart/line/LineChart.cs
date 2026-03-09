using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 折线图组件，使用 DataSource 中的序列数据进行绘制。
/// </summary>
[GlobalClass]
public partial class LineChart : Control
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

    // 当前悬浮点。
    private InteractivePoint? hoveredPoint;

    // 悬浮提示面板。
    private PanelContainer tooltipPanel = null!;

    // 悬浮提示文本。
    private Label tooltipLabel = null!;

    private sealed record InteractivePoint(Vector2 Position, string XText, string YText, Color Color);

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
        QueueRedraw();
    }

    /// <summary>
    /// 设置并刷新图表配置。
    /// </summary>
    /// <param name="chart">图表配置对象。</param>
    public void UpdateChart(Chart chart)
    {
        this.chart = chart;
        QueueRedraw();
    }

    /// <summary>
    /// 绘制控件内容。
    /// </summary>
    public override void _Draw()
    {
        interactivePoints = [];

        Color labelColor = GetThemeColor("font_color", "Label");
        Font? font = GetThemeDefaultFont();
        int fontSize = GetThemeDefaultFontSize();
        DataSource dataSource = chart.DataSource;

        if (!string.IsNullOrEmpty(dataSource.Title) && font is not null)
        {
            DrawString(font, new Vector2(0, TitleHeight - 6f), dataSource.Title, HorizontalAlignment.Left, -1, fontSize, labelColor);
        }

        Rect2 plotRect = new Rect2(
            PlotPaddingLeft,
            TitleHeight + PlotPaddingTop,
            Size.X - PlotPaddingLeft - PlotPaddingRight,
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
        if (@event is not InputEventMouseMotion mouseMotion)
        {
            return;
        }

        updateHoveredPoint(mouseMotion.Position);
    }

    // 绘制所有折线序列。
    private void drawSeries(Rect2 plotRect, PlotBounds bounds)
    {
        DataSource dataSource = chart.DataSource;
        List<DataSeries> seriesList = dataSource.SeriesList.ToList();
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
                List<PointState> pointStates = Enumerable.Range(0, series.Values.Count)
                    .Select(index => createPointState(series, index, plotRect, bounds))
                    .ToList();

                if (pointStates.Count > 1)
                {
                    Enumerable.Range(0, pointStates.Count - 1)
                        .Where(index => pointStates[index].Visible && pointStates[index + 1].Visible)
                        .ToList()
                        .ForEach(index => DrawLine(pointStates[index].Position, pointStates[index + 1].Position, seriesColor, 2f, true));
                }

                pointStates
                    .Where(state => state.Visible)
                    .ToList()
                    .ForEach(state =>
                    {
                        DrawCircle(state.Position, PointRadius, seriesColor);
                        points.Add(new InteractivePoint(state.Position, state.XText, state.YText, seriesColor));
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
        List<DataSeries> seriesList = dataSource.SeriesList.ToList();
        int maxPointCount = seriesList.Select(series => series.Values.Count).DefaultIfEmpty(0).Max();

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
                string xText = index < dataSource.XLabels.Count
                    ? dataSource.XLabels[index]
                    : chart.XAxis.Format(xValue);

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
        bool visible = isPointVisible(xValue, yValue, bounds);
        Vector2 position = new Vector2(mapX(plotRect, bounds, xValue), mapY(plotRect, bounds, yValue));
        string xText = index < dataSource.XLabels.Count
            ? dataSource.XLabels[index]
            : chart.XAxis.Format(xValue);
        string yText = chart.YAxis.Format(yValue);

        return new PointState(position, xText, yText, visible);
    }

    // 根据鼠标位置更新当前悬浮点。
    private void updateHoveredPoint(Vector2 mousePosition)
    {
        InteractivePoint? nearestPoint = interactivePoints
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
        List<DataSeries> seriesList = dataSource.SeriesList.ToList();
        if (seriesList.Count == 0)
        {
            return null;
        }

        int maxPointCount = seriesList.Select(series => series.Values.Count).DefaultIfEmpty(0).Max();
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

    // 获取 X 值（优先使用 DataSource.XValues）。
    private static float getXValue(DataSource dataSource, int index)
    {
        return index < dataSource.XValues.Count
            ? dataSource.XValues[index]
            : index;
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