using Godot;
using System;
using System.Diagnostics;

/// <summary>
/// 负责节点拖拽行为的交互控制器，依赖 CanvasView 信号驱动。
/// </summary>
public sealed class NodeDraggable
{
    // 画布视图。
    private CanvasView canvasView = null!;
    // 画布数据。
    private TopologyCanvas topologyCanvas = null!;
    // 当前拖拽状态。
    private bool isDragging;
    // 当前拖拽节点 ID。
    private string draggingNodeId = string.Empty;
    // 指针与节点左上角的偏移。
    private Vector2 dragPointerOffset = Vector2.Zero;

    /// <summary>
    /// 绑定拖拽控制器到画布视图与画布数据。
    /// </summary>
    public NodeDraggable(CanvasView inputCanvasView, TopologyCanvas inputTopologyCanvas)
    {
        canvasView = inputCanvasView;
        topologyCanvas = inputTopologyCanvas;
        canvasView.NodeSelect += onNodeSelected;
        canvasView.MouseMotionInputRecognized += onMouseMotionInputRecognized;
        canvasView.MouseButtonInputRecognized += onMouseButtonInputRecognized;
    }

    // 节点选中后进入拖拽准备状态。
    private void onNodeSelected(string nodeId, Vector2 graphPointerPosition)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            isDragging = false;
            draggingNodeId = string.Empty;
            dragPointerOffset = Vector2.Zero;
            return;
        }

        Debug.Assert(topologyCanvas.Nodes.ContainsKey(nodeId));
        TopologyNode selectedNode = topologyCanvas.Nodes[nodeId];
        draggingNodeId = nodeId;
        dragPointerOffset = graphPointerPosition - selectedNode.Position;
        isDragging = true;
        canvasView.SetDropShadow(nodeId, selectedNode.Position);
    }

    // 响应鼠标移动，持续更新拖拽中的节点位置。
    private void onMouseMotionInputRecognized(InputEventMouseMotion mouseMotion)
    {
        if (isDragging == false)
        {
            return;
        }

        if ((mouseMotion.ButtonMask & MouseButtonMask.Left) == 0)
        {
            finishDragging(mouseMotion.Position);
            return;
        }

        updateDraggingNodePosition(mouseMotion.Position);
    }

    // 响应鼠标按钮，识别拖拽结束时机。
    private void onMouseButtonInputRecognized(InputEventMouseButton mouseButton)
    {
        if (mouseButton.ButtonIndex != MouseButton.Left || mouseButton.Pressed)
        {
            return;
        }

        if (isDragging)
        {
            finishDragging(mouseButton.Position);
        }
    }

    // 完成拖拽并将阴影定位到最终位置。
    private void finishDragging(Vector2 canvasLocalPointerPosition)
    {
        if (string.IsNullOrWhiteSpace(draggingNodeId))
        {
            isDragging = false;
            return;
        }

        updateDraggingNodePosition(canvasLocalPointerPosition);
        isDragging = false;
    }

    // 根据当前指针位置刷新节点坐标。
    private void updateDraggingNodePosition(Vector2 canvasLocalPointerPosition)
    {
        if (string.IsNullOrWhiteSpace(draggingNodeId))
        {
            return;
        }

        Vector2 pointerGraphPosition = canvasView.MapCanvasLocalPointerToGraph(canvasLocalPointerPosition);
        Vector2 targetNodePosition = pointerGraphPosition - dragPointerOffset;
        topologyCanvas.UpdateNodePosition(draggingNodeId, targetNodePosition);
        canvasView.SetDropShadow(draggingNodeId, targetNodePosition);
        canvasView.QueueRedraw();
    }
}
