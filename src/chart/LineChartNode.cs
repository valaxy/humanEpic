using Godot;

/// <summary>
/// 折线图组件，使用 DataSource 中的序列数据进行绘制。
/// </summary>
[GlobalClass]
public partial class LineChartNode : Control
{
    private const float ChartPadding = 24f;
    private const float TitleHeight = 24f;
    private const float LabelHeight = 20f;

    private DataSource _dataSource = new();

    public void Render(DataSource dataSource)
    {
        _dataSource = dataSource;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var labelColor = GetThemeColor("font_color", "Label");
        var font = GetThemeDefaultFont();
        var fontSize = GetThemeDefaultFontSize();

        if (!string.IsNullOrEmpty(_dataSource.Title) && font is not null)
        {
            DrawString(font, new Vector2(0, TitleHeight - 6f), _dataSource.Title, HorizontalAlignment.Left, -1, fontSize, labelColor);
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
        DrawSeries(plotRect);
        DrawXLabels(plotRect, font, fontSize, labelColor);
    }

    private void DrawSeries(Rect2 plotRect)
    {
        if (_dataSource.SeriesList.Count == 0)
        {
            return;
        }

        var min = float.MaxValue;
        var max = float.MinValue;
        var maxPointCount = 0;

        foreach (var series in _dataSource.SeriesList)
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

        foreach (var series in _dataSource.SeriesList)
        {
            if (series.Values.Count <= 1)
            {
                continue;
            }

            for (var i = 0; i < series.Values.Count - 1; i++)
            {
                var start = GetPoint(plotRect, i, series.Values[i], xStep, min, valueRange);
                var end = GetPoint(plotRect, i + 1, series.Values[i + 1], xStep, min, valueRange);
                DrawLine(start, end, series.Color, 2f, true);
            }
        }
    }

    private void DrawXLabels(Rect2 plotRect, Font? font, int fontSize, Color color)
    {
        if (font is null || _dataSource.XLabels.Count == 0)
        {
            return;
        }

        if (_dataSource.XLabels.Count == 1)
        {
            DrawString(font, new Vector2(plotRect.Position.X, plotRect.End.Y + LabelHeight), _dataSource.XLabels[0], HorizontalAlignment.Left, -1, fontSize, color);
            return;
        }

        var xStep = plotRect.Size.X / (_dataSource.XLabels.Count - 1);
        for (var i = 0; i < _dataSource.XLabels.Count; i++)
        {
            var x = plotRect.Position.X + i * xStep;
            DrawString(font, new Vector2(x, plotRect.End.Y + LabelHeight), _dataSource.XLabels[i], HorizontalAlignment.Center, 90, fontSize, color);
        }
    }

    private static Vector2 GetPoint(Rect2 plotRect, int index, float value, float xStep, float min, float range)
    {
        var x = plotRect.Position.X + index * xStep;
        var ratio = (value - min) / range;
        var y = plotRect.End.Y - ratio * plotRect.Size.Y;
        return new Vector2(x, y);
    }
}