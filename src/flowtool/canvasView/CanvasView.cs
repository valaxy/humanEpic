using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 画布世界渲染层，负责背景、节点、连线与拖拽阴影绘制。
/// </summary>
[Tool]
[GlobalClass]
public partial class CanvasView : Node2D
{
	/// <summary>
	/// 鼠标按钮输入识别信号。
	/// </summary>
	[Signal]
	public delegate void MouseButtonInputRecognizedEventHandler(InputEventMouseButton mouseButton);

	/// <summary>
	/// 鼠标移动输入识别信号。
	/// </summary>
	[Signal]
	public delegate void MouseMotionInputRecognizedEventHandler(InputEventMouseMotion mouseMotion);

	/// <summary>
	/// 节点拖拽落点信号。
	/// </summary>
	[Signal]
	public delegate void NodePayloadDroppedEventHandler(string nodeId, Vector2 graphPosition);

	/// <summary>
	/// 节点选中识别信号。
	/// </summary>
	[Signal]
	public delegate void NodeSelectedRecognizedEventHandler(string nodeId, Vector2 graphPointerPosition);

	/// <summary>
	/// 选中节点删除请求信号。
	/// </summary>
	[Signal]
	public delegate void SelectedNodeDeleteRequestedEventHandler(string nodeId);


	// 画布背景色。
	private static readonly Color canvasBackgroundColor = new(0.11f, 0.14f, 0.18f);
	// 画布边框色。
	private static readonly Color canvasBorderColor = new(0.24f, 0.31f, 0.39f);
	// 拖拽阴影填充色。
	private static readonly Color dropShadowFillColor = new(0.78f, 0.86f, 0.98f, 0.18f);
	// 拖拽阴影边框色。
	private static readonly Color dropShadowBorderColor = new(0.32f, 0.62f, 0.94f, 0.9f);
	// 删除按钮填充色。
	private static readonly Color deleteButtonFillColor = new(0.88f, 0.26f, 0.26f, 0.95f);
	// 删除按钮边框色。
	private static readonly Color deleteButtonBorderColor = new(1f, 0.93f, 0.93f, 0.95f);
	// 删除按钮图标色。
	private static readonly Color deleteButtonIconColor = new(1f, 1f, 1f, 0.95f);
	// 删除按钮尺寸。
	private const float deleteButtonSize = 22f;
	// 删除按钮边距。
	private const float deleteButtonMargin = 6f;
	// 输入源画布容器。
	private SubViewportContainer canvasPanel = null!;
	// 主视口。
	private SubViewport mainViewport = null!;
	// 主摄像机。
	private Camera2D mainCamera = null!;
	// 画布领域模型。
	private TopologyCanvas topologyCanvas = null!;
	// 当前选中节点 ID。
	private string selectedNodeId = string.Empty;
	// 拖拽阴影节点 ID。
	private string dropShadowNodeId = string.Empty;
	// 拖拽阴影位置。
	private Vector2 dropShadowPosition = Vector2.Zero;



	public override void _Ready()
	{
		canvasPanel = GetNode<SubViewportContainer>("../../../");
		mainViewport = GetNode<SubViewport>("../../");
		mainCamera = GetNode<Camera2D>("../MainCamera");
		canvasPanel.GuiInput += handleInputEvent;
		configureMainViewport();
		SyncViewportSize();
	}

	// 配置主视口。
	private void configureMainViewport()
	{
		mainViewport.World2D ??= new World2D();
		mainViewport.HandleInputLocally = true;
		mainViewport.TransparentBg = false;
		mainViewport.Size2DOverrideStretch = false;
		mainCamera.Enabled = true;
		mainCamera.MakeCurrent();
	}

	/// <summary>
	/// 初始化渲染层依赖的画布领域模型。
	/// </summary>
	public void Initialize(TopologyCanvas topologyCanvas)
	{
		this.topologyCanvas = topologyCanvas;
		QueueRedraw();
	}





	/// <summary>
	/// 获取画布容器的当前像素尺寸。
	/// </summary>
	public Vector2I GetCanvasViewportSize()
	{
		return new Vector2I(
			Mathf.Max(Mathf.RoundToInt(canvasPanel.Size.X), 1),
			Mathf.Max(Mathf.RoundToInt(canvasPanel.Size.Y), 1));
	}

	/// <summary>
	/// 将全局指针坐标映射到画布局部坐标。
	/// </summary>
	public bool TryMapGlobalPointerToCanvas(Vector2 pointerGlobalPosition, out Vector2 canvasLocalPointerPosition)
	{
		canvasLocalPointerPosition = Vector2.Zero;
		Rect2 canvasGlobalRect = canvasPanel.GetGlobalRect();
		if (canvasGlobalRect.HasPoint(pointerGlobalPosition) == false)
		{
			return false;
		}

		canvasLocalPointerPosition = canvasPanel.GetGlobalTransformWithCanvas().AffineInverse() * pointerGlobalPosition;
		return true;
	}

	/// <summary>
	/// 同步主视口尺寸到画布容器大小。
	/// </summary>
	public void SyncViewportSize()
	{
		mainViewport.Size = GetCanvasViewportSize();
	}

	/// <summary>
	/// 尝试处理缩放输入。
	/// </summary>
	public bool TryHandleZoom(CanvasZoomController zoomController, InputEventMouseButton mouseButton)
	{
		return zoomController.TryHandle(mouseButton, mainCamera);
	}

	/// <summary>
	/// 更新当前选中节点样式。
	/// </summary>
	public void UpdateSelectedNode(string nodeId)
	{
		selectedNodeId = nodeId;
		QueueRedraw();
	}

	/// <summary>
	/// 应用节点与连线图快照并触发重绘。
	/// </summary>
	public void RenderGraph(
		IReadOnlyDictionary<string, MetricNode> nodesByNodeId,
		IReadOnlyDictionary<string, Vector2> layoutByNodeId,
		IReadOnlyList<MetricEdge> activeEdges,
		string selectedNodeId)
	{
		topologyCanvas.UpdateGraph(nodesByNodeId, layoutByNodeId, activeEdges);
		this.selectedNodeId = selectedNodeId;
		QueueRedraw();
	}

	/// <summary>
	/// 清空图快照并触发重绘。
	/// </summary>
	public void ClearGraph(string selectedNodeId)
	{
		topologyCanvas.UpdateGraph(
			new Dictionary<string, MetricNode>(StringComparer.Ordinal),
			new Dictionary<string, Vector2>(StringComparer.Ordinal),
			Array.Empty<MetricEdge>());
		this.selectedNodeId = selectedNodeId;
		QueueRedraw();
	}


	/// <summary>
	/// 更新拖拽阴影节点显示。
	/// </summary>
	public void SetDropShadow(string nodeId, Vector2 position)
	{
		dropShadowNodeId = nodeId;
		dropShadowPosition = position;
		QueueRedraw();
	}

	/// <summary>
	/// 清除拖拽阴影节点显示。
	/// </summary>
	public void ClearDropShadow()
	{
		dropShadowNodeId = string.Empty;
		dropShadowPosition = Vector2.Zero;
		QueueRedraw();
	}



	// 输入相关

	// 识别输入事件并转发为信号，不在视图内处理业务。
	private void handleInputEvent(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
			{
				Vector2 graphPointerPosition = mapCanvasLocalPointerToGraph(mouseButton.Position);
				if (tryGetSelectedNodeDeleteButtonRect(out Rect2 deleteButtonRect) && deleteButtonRect.HasPoint(graphPointerPosition)
					&& tryEmitNodeDeleteSignal())
				{
					return;
				}

				topologyCanvas.TryPickNodeIdAt(graphPointerPosition, out string nodeId);
				EmitSignal(SignalName.NodeSelectedRecognized, nodeId, graphPointerPosition);
			}

			EmitSignal(SignalName.MouseButtonInputRecognized, mouseButton);
			return;
		}

		// 移动鼠标时触发
		if (@event is InputEventMouseMotion mouseMotion)
		{
			EmitSignal(SignalName.MouseMotionInputRecognized, mouseMotion);
			return;
		}

		// 这个if分支不能删，成功抛出删除事件
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Delete && tryEmitNodeDeleteSignal())
		{
			return;
		}
	}

	private bool tryEmitNodeDeleteSignal()
	{
		if (string.IsNullOrWhiteSpace(selectedNodeId))
		{
			return false;
		}

		EmitSignal(SignalName.SelectedNodeDeleteRequested, selectedNodeId);
		return true;
	}


	// 将画布局部坐标映射到画布世界坐标。
	private Vector2 mapCanvasLocalPointerToGraph(Vector2 canvasLocalPointerPosition)
	{
		return mainViewport.CanvasTransform.AffineInverse() * canvasLocalPointerPosition;
	}



	// 绘制相关

	public override void _Draw()
	{
		// drawCanvasBackground();
		CanvasEdgePainter.DrawEdges(this, topologyCanvas);
		CanvasNodePainter.DrawNodes(this, topologyCanvas, selectedNodeId);
		drawSelectedNodeDeleteButton();
		drawDropShadow();
	}

	// 绘制画布背景与边框。
	private void drawCanvasBackground()
	{
		Rect2 canvasRect = new(Vector2.Zero, topologyCanvas.CanvasSize);
		DrawRect(canvasRect, canvasBackgroundColor, true);
		DrawRect(canvasRect, canvasBorderColor, false, 3f);
	}

	// 绘制拖拽阴影。
	private void drawDropShadow()
	{
		if (string.IsNullOrWhiteSpace(dropShadowNodeId))
		{
			return;
		}

		Rect2 shadowRect = new(dropShadowPosition, topologyCanvas.NodeSize);
		DrawRect(shadowRect, dropShadowFillColor, true);
		DrawRect(shadowRect, dropShadowBorderColor, false, 2f);
	}

	// 绘制选中节点右上角删除按钮。
	private void drawSelectedNodeDeleteButton()
	{
		if (tryGetSelectedNodeDeleteButtonRect(out Rect2 deleteButtonRect) == false)
		{
			return;
		}

		DrawRect(deleteButtonRect, deleteButtonFillColor, true);
		DrawRect(deleteButtonRect, deleteButtonBorderColor, false, 1.5f);
		Vector2 topLeft = deleteButtonRect.Position + new Vector2(6f, 6f);
		Vector2 bottomRight = deleteButtonRect.End - new Vector2(6f, 6f);
		Vector2 topRight = new(deleteButtonRect.End.X - 6f, deleteButtonRect.Position.Y + 6f);
		Vector2 bottomLeft = new(deleteButtonRect.Position.X + 6f, deleteButtonRect.End.Y - 6f);
		DrawLine(topLeft, bottomRight, deleteButtonIconColor, 2f, true);
		DrawLine(topRight, bottomLeft, deleteButtonIconColor, 2f, true);
	}

	// 计算当前选中节点的删除按钮区域。
	private bool tryGetSelectedNodeDeleteButtonRect(out Rect2 deleteButtonRect)
	{
		deleteButtonRect = default;
		if (string.IsNullOrWhiteSpace(selectedNodeId))
		{
			return false;
		}

		if (topologyCanvas.NodeLayout.ContainsKey(selectedNodeId) == false)
		{
			return false;
		}

		Vector2 nodePosition = topologyCanvas.NodeLayout[selectedNodeId];
		Vector2 buttonPosition = new(
			nodePosition.X + topologyCanvas.NodeWidth - deleteButtonSize - deleteButtonMargin,
			nodePosition.Y + deleteButtonMargin);
		deleteButtonRect = new Rect2(buttonPosition, new Vector2(deleteButtonSize, deleteButtonSize));
		return true;
	}
}
