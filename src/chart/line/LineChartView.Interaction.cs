using Godot;
using System;
using System.Linq;

public partial class LineChartView
{
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

    // 更新图例悬浮项。
    private void updateHoveredLegend(Vector2 mousePosition)
    {
        string? nextHoveredLegendKey = legendRenderer.ResolveHoveredLegendKey(interactiveLegendItems, mousePosition);
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
        string? legendKey = legendRenderer.ResolveToggleLegendKey(interactiveLegendItems, mousePosition);
        if (string.IsNullOrWhiteSpace(legendKey))
        {
            return;
        }

        bool currentVisible = legendVisibility.TryGetValue(legendKey, out bool value) && value;
        legendVisibility[legendKey] = !currentVisible;

        if (hoveredPoint != null && hoveredPoint.SeriesKey == legendKey && !legendVisibility[legendKey])
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
}
