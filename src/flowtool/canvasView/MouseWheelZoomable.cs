using Godot;

/// <summary>
/// 负责鼠标滚轮缩放行为的交互控制器，依赖 CanvasView 信号驱动。
/// </summary>
public sealed class MouseWheelZoomable
{
    // 最小缩放比。
    private const float minZoom = 0.35f;
    // 最大缩放比。
    private const float maxZoom = 3.0f;
    // 向内缩放倍率。
    private const float zoomInFactor = 0.9f;
    // 向外缩放倍率。
    private const float zoomOutFactor = 1.1f;
    // 画布视图。
    private CanvasView canvasView = null!;

    /// <summary>
    /// 绑定缩放控制器到画布视图。
    /// </summary>
    public MouseWheelZoomable(CanvasView inputCanvasView)
    {
        canvasView = inputCanvasView;
        canvasView.MouseButtonInputRecognized += onMouseButtonInputRecognized;
    }

    // 响应滚轮输入并进行缩放。
    private void onMouseButtonInputRecognized(InputEventMouseButton mouseButton)
    {
        if (mouseButton.Pressed == false)
        {
            return;
        }

        if (mouseButton.ButtonIndex != MouseButton.WheelUp && mouseButton.ButtonIndex != MouseButton.WheelDown)
        {
            return;
        }

        float zoomFactor = mouseButton.ButtonIndex == MouseButton.WheelUp ? zoomInFactor : zoomOutFactor;
        applyZoom(mouseButton.Position, zoomFactor);
    }

    // 以鼠标指针所在图坐标为锚点进行缩放。
    private void applyZoom(Vector2 canvasLocalPointerPosition, float zoomFactor)
    {
        Vector2 beforeZoomPointerGraphPosition = canvasView.MapCanvasLocalPointerToGraph(canvasLocalPointerPosition);
        Vector2 currentZoom = canvasView.CameraZoom;
        float targetZoomX = Mathf.Clamp(currentZoom.X * zoomFactor, minZoom, maxZoom);
        float targetZoomY = Mathf.Clamp(currentZoom.Y * zoomFactor, minZoom, maxZoom);
        canvasView.CameraZoom = new Vector2(targetZoomX, targetZoomY);
        Vector2 afterZoomPointerGraphPosition = canvasView.MapCanvasLocalPointerToGraph(canvasLocalPointerPosition);
        Vector2 anchorShift = beforeZoomPointerGraphPosition - afterZoomPointerGraphPosition;
        canvasView.CameraPosition += anchorShift;
        canvasView.QueueRedraw();
    }
}
