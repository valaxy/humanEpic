using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LineChartView
{
    // 绘制所有折线序列。
    private void drawSeries(Rect2 plotRect, PlotBounds bounds)
    {
        List<DataSeries> seriesList = getVisibleSeries();
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
                string seriesKey = LineChartDataSourceFactory.ResolveSeriesKey(series);
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
        List<LineAxisPoint> axisPoints = chart.AxisPoints.ToList();
        int maxPointCount = axisPoints.Count;

        if (maxPointCount == 0)
        {
            return;
        }

        List<int> visibleIndices = Enumerable.Range(0, maxPointCount)
            .Where(index => chart.XAxis.IsInRange(getXValue(axisPoints, index)))
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
                float xValue = getXValue(axisPoints, index);
                float x = mapX(plotRect, bounds, xValue);
                string xText = getXText(axisPoints, index);

                DrawLine(new Vector2(x, plotRect.End.Y), new Vector2(x, plotRect.End.Y + 4f), color, 1f, true);
                DrawString(font, new Vector2(x, plotRect.End.Y + fontSize + 2f), xText, HorizontalAlignment.Center, 120f, fontSize - 1, color);
            });
    }

    // 创建一个点状态。
    private PointState createPointState(DataSeries series, int index, Rect2 plotRect, PlotBounds bounds)
    {
        List<LineAxisPoint> axisPoints = chart.AxisPoints.ToList();
        float xValue = getXValue(axisPoints, index);
        float yValue = series.Values[index];
        bool visible = !float.IsNaN(yValue) && isPointVisible(xValue, yValue, bounds);
        Vector2 position = new Vector2(mapX(plotRect, bounds, xValue), mapY(plotRect, bounds, yValue));
        string xText = getXText(axisPoints, index);
        string yText = chart.YAxis.Format(yValue);

        return new PointState(position, xText, yText, visible);
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
        List<DataSeries> seriesList = getVisibleSeries();
        if (seriesList.Count == 0)
        {
            return null;
        }

        int maxPointCount = chart.AxisPoints.Count;
        if (maxPointCount == 0)
        {
            return null;
        }

        List<float> visibleXValues = Enumerable.Range(0, maxPointCount)
            .Select(index => getXValue(chart.AxisPoints, index))
            .Where(value => chart.XAxis.IsInRange(value))
            .ToList();

        if (visibleXValues.Count == 0)
        {
            return null;
        }

        List<float> visibleYValues = seriesList
            .SelectMany(series => Enumerable.Range(0, series.Values.Count)
                .Where(index => chart.XAxis.IsInRange(getXValue(chart.AxisPoints, index)))
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
    private static float getXValue(IReadOnlyList<LineAxisPoint> axisPoints, int index)
    {
        if (index >= 0 && index < axisPoints.Count)
        {
            return axisPoints[index].Value;
        }

        return index;
    }

    // 获取 X 轴显示文本。
    private string getXText(IReadOnlyList<LineAxisPoint> axisPoints, int index)
    {
        if (index >= 0 && index < axisPoints.Count)
        {
            return axisPoints[index].Label;
        }

        return chart.XAxis.Format(index);
    }

    // 获取当前可见序列。
    private List<DataSeries> getVisibleSeries()
    {
        return legendRenderer.GetVisibleSeries(chart, legendVisibility);
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
