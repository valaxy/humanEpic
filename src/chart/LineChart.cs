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
    // 图表区域内边距。
    private const float ChartPadding = 24f;

    // 标题区域高度。
    private const float TitleHeight = 24f;

    // X 轴标签区域高度。
    private const float LabelHeight = 20f;

    // 当前渲染数据源。
    private DataSource dataSource = DataSource.CreateLineChart(string.Empty, Array.Empty<string>(), Array.Empty<DataSeries>());

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
        Color labelColor = GetThemeColor("font_color", "Label");
        Font? font = GetThemeDefaultFont();
        int fontSize = GetThemeDefaultFontSize();

        if (!string.IsNullOrEmpty(dataSource.Title) && font is not null)
        {
            DrawString(font, new Vector2(0, TitleHeight - 6f), dataSource.Title, HorizontalAlignment.Left, -1, fontSize, labelColor);
        }

        Rect2 plotRect = new Rect2(
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
        if (font != null)
        {
            drawXLabels(plotRect, font, fontSize, labelColor);
        }
    }

    // 绘制所有折线序列。
    private void drawSeries(Rect2 plotRect)
    {
        if (dataSource.SeriesList.Count == 0)
        {
            return;
        }

        List<DataSeries> seriesList = dataSource.SeriesList.ToList();
        int maxPointCount = seriesList.Select(series => series.Values.Count).DefaultIfEmpty(0).Max();
        List<float> allValues = seriesList.SelectMany(series => series.Values).ToList();
        float min = allValues.Count > 0 ? allValues.Min() : 0.0f;
        float max = allValues.Count > 0 ? allValues.Max() : 0.0f;

        if (maxPointCount <= 1)
        {
            return;
        }

        if (Mathf.IsEqualApprox(min, max))
        {
            min -= 1f;
            max += 1f;
        }

        float xStep = plotRect.Size.X / (maxPointCount - 1);
        float valueRange = max - min;

        seriesList
            .Where(series => series.Values.Count > 1)
            .ToList()
            .ForEach(series =>
            {
                Color seriesColor = toGodotColor(series.ColorHex);
                Enumerable.Range(0, series.Values.Count - 1)
                    .ToList()
                    .ForEach(index =>
                    {
                        Vector2 start = getPoint(plotRect, index, series.Values[index], xStep, min, valueRange);
                        Vector2 end = getPoint(plotRect, index + 1, series.Values[index + 1], xStep, min, valueRange);
                        DrawLine(start, end, seriesColor, 2f, true);
                    });
            });
    }

    // 绘制 X 轴标签。
    private void drawXLabels(Rect2 plotRect, Font font, int fontSize, Color color)
    {
        if (dataSource.XLabels.Count == 0)
        {
            return;
        }

        if (dataSource.XLabels.Count == 1)
        {
            DrawString(font, new Vector2(plotRect.Position.X, plotRect.End.Y + LabelHeight), dataSource.XLabels[0], HorizontalAlignment.Left, -1, fontSize, color);
            return;
        }

        float xStep = plotRect.Size.X / (dataSource.XLabels.Count - 1);
        Enumerable.Range(0, dataSource.XLabels.Count)
            .ToList()
            .ForEach(index =>
            {
                float x = plotRect.Position.X + index * xStep;
                DrawString(font, new Vector2(x, plotRect.End.Y + LabelHeight), dataSource.XLabels[index], HorizontalAlignment.Center, 90, fontSize, color);
            });
    }

    // 将序列值映射为绘制坐标点。
    private static Vector2 getPoint(Rect2 plotRect, int index, float value, float xStep, float min, float range)
    {
        float x = plotRect.Position.X + index * xStep;
        float ratio = (value - min) / range;
        float y = plotRect.End.Y - ratio * plotRect.Size.Y;
        return new Vector2(x, y);
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