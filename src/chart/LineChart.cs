using Godot;

/// <summary>
/// 折线图组件，使用 DataSource 中的序列数据进行绘制。
/// </summary>
[GlobalClass]
public partial class LineChart : Control
{
    // 图表区域内边距。
    private const float ChartPadding = 24f;

    // 标题区域高度。
    private const float TitleHeight = 24f;

    // X 轴标签区域高度。
    private const float LabelHeight = 20f;

    // 当前渲染数据源。
    private DataSource dataSource = new();

    /// <summary>
    /// 设置并渲染折线图数据源。
    /// </summary>
    /// <param name="dataSource">图表数据源。</param>
    public void Render(DataSource dataSource)
    {
        this.dataSource = dataSource;
        QueueRedraw();
    }

    /// <summary>
    /// 绘制控件内容。
    /// </summary>
    public override void _Draw()
    {
        var labelColor = GetThemeColor("font_color", "Label");
        var font = GetThemeDefaultFont();
        var fontSize = GetThemeDefaultFontSize();

        if (!string.IsNullOrEmpty(dataSource.Title) && font is not null)
        {
            DrawString(font, new Vector2(0, TitleHeight - 6f), dataSource.Title, HorizontalAlignment.Left, -1, fontSize, labelColor);
        }

        var plotRect = new Rect2(
            ChartPadding,
            TitleHeight,
            Size.X - ChartPadding * 2,
            Size.Y - TitleHeight - LabelHeight - ChartPadding);

        if (plotRect.Size.X <= 0 || plotRect.Size.Y <= 0)
        {
            return;
        }

        DrawRect(plotRect, labelColor, false, 1f);
        drawSeries(plotRect);
        drawXLabels(plotRect, font, fontSize, labelColor);
    }

    // 绘制所有折线序列。
    private void drawSeries(Rect2 plotRect)
    {
        if (dataSource.SeriesList.Count == 0)
        {
            return;
        }

        var min = float.MaxValue;
        var max = float.MinValue;
        var maxPointCount = 0;

        foreach (var series in dataSource.SeriesList)
        {
            maxPointCount = Mathf.Max(maxPointCount, series.Values.Count);
            foreach (var value in series.Values)
            {
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
        }

        if (maxPointCount <= 1)
        {
            return;
        }

        if (Mathf.IsEqualApprox(min, max))
        {
            min -= 1f;
            max += 1f;
        }

        var xStep = plotRect.Size.X / (maxPointCount - 1);
        var valueRange = max - min;

        foreach (var series in dataSource.SeriesList)
        {
            if (series.Values.Count <= 1)
            {
                continue;
            }

            for (var i = 0; i < series.Values.Count - 1; i++)
            {
                var start = getPoint(plotRect, i, series.Values[i], xStep, min, valueRange);
                var end = getPoint(plotRect, i + 1, series.Values[i + 1], xStep, min, valueRange);
                DrawLine(start, end, series.Color, 2f, true);
            }
        }
    }

    // 绘制 X 轴标签。
    private void drawXLabels(Rect2 plotRect, Font? font, int fontSize, Color color)
    {
        if (font is null || dataSource.XLabels.Count == 0)
        {
            return;
        }

        if (dataSource.XLabels.Count == 1)
        {
            DrawString(font, new Vector2(plotRect.Position.X, plotRect.End.Y + LabelHeight), dataSource.XLabels[0], HorizontalAlignment.Left, -1, fontSize, color);
            return;
        }

        var xStep = plotRect.Size.X / (dataSource.XLabels.Count - 1);
        for (var i = 0; i < dataSource.XLabels.Count; i++)
        {
            var x = plotRect.Position.X + i * xStep;
            DrawString(font, new Vector2(x, plotRect.End.Y + LabelHeight), dataSource.XLabels[i], HorizontalAlignment.Center, 90, fontSize, color);
        }
    }

    // 将序列值映射为绘制坐标点。
    private static Vector2 getPoint(Rect2 plotRect, int index, float value, float xStep, float min, float range)
    {
        var x = plotRect.Position.X + index * xStep;
        var ratio = (value - min) / range;
        var y = plotRect.End.Y - ratio * plotRect.Size.Y;
        return new Vector2(x, y);
    }
}