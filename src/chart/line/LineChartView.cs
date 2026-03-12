using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 折线图组件，使用 Chart 中的数据进行绘制。
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

    // 悬浮点半径。
    private const float PointRadius = 3.5f;

    // 悬浮命中半径。
    private const float HoverRadius = 9f;

    // 当前图表配置。
    private Chart chart = LineChartDataSourceFactory.Create(string.Empty, Array.Empty<string>(), Array.Empty<DataSeries>());

    // 图例渲染与交互处理器。
    private LineChartLegendRenderer legendRenderer = new();

    // 当前可交互点集合。
    private List<InteractivePoint> interactivePoints = [];

    // 当前可交互图例集合。
    private List<LineChartLegendInteractiveItem> interactiveLegendItems = [];

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
    /// 设置并渲染图表。
    /// </summary>
    /// <param name="chart">图表配置对象。</param>
    public void Render(Chart chart)
    {
        UpdateChart(chart);
    }

    /// <summary>
    /// 设置并刷新图表配置。
    /// </summary>
    /// <param name="chart">图表配置对象。</param>
    public void UpdateChart(Chart chart)
    {
        this.chart = chart;
        legendRenderer.SyncLegendState(chart, ref legendVisibility, ref hoveredLegendKey);
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
        legendRenderer.SyncLegendState(chart, ref legendVisibility, ref hoveredLegendKey);

        if (!string.IsNullOrEmpty(chart.DataSource.Title) && font is not null)
        {
            DrawString(font, new Vector2(0, TitleHeight - 6f), chart.DataSource.Title, HorizontalAlignment.Left, -1, fontSize, labelColor);
        }

        bool hasLegend = legendRenderer.HasLegend(chart);
        float legendAreaWidth = hasLegend ? legendRenderer.LegendAreaWidth : 0f;

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
                interactiveLegendItems = legendRenderer.DrawLegend(this, plotRect, font, fontSize, labelColor, chart, legendVisibility, hoveredLegendKey);
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
}
