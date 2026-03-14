using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 画布渲染层，只负责渲染与输入识别。
/// </summary>
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
	/// 节点选中识别信号。
	/// </summary>
	[Signal]
	public delegate void NodeSelectEventHandler(string nodeId, Vector2 graphPointerPosition);

	/// <summary>
	/// 选中节点删除请求信号。
	/// </summary>
	[Signal]
	public delegate void SelectedNodeDeleteEventHandler(string nodeId);


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
	private TopologyCanvas topologyCanvas = TopologyCanvas.Instance;
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
		initMainViewport();
		initViewportSize();
	}

	// 配置主视口。
	private void initMainViewport()
	{
		mainViewport.World2D ??= new World2D();
		mainViewport.HandleInputLocally = true;
		mainViewport.TransparentBg = false;
		mainViewport.Size2DOverrideStretch = false;
		mainCamera.Enabled = true;
		mainCamera.MakeCurrent();
	}

	// 同步主视口尺寸到画布容器大小。
	private void initViewportSize()
	{
		mainViewport.Size = new Vector2I(
			Mathf.RoundToInt(canvasPanel.Size.X),
			Mathf.RoundToInt(canvasPanel.Size.Y)
		);
	}

	/// <summary>
	/// 初始化渲染层依赖的画布领域模型。
	/// </summary>
	public void Setup(TopologyCanvas inputTopologyCanvas)
	{
		topologyCanvas = inputTopologyCanvas;
		QueueRedraw();
	}











	// /// <summary>
	// /// 将全局指针坐标映射到画布局部坐标。
	// /// </summary>
	// public bool TryMapGlobalPointerToCanvas(Vector2 pointerGlobalPosition, out Vector2 canvasLocalPointerPosition)
	// {
	// 	canvasLocalPointerPosition = Vector2.Zero;
	// 	Rect2 canvasGlobalRect = canvasPanel.GetGlobalRect();
	// 	if (canvasGlobalRect.HasPoint(pointerGlobalPosition) == false)
	// 	{
	// 		return false;
	// 	}

	// 	canvasLocalPointerPosition = canvasPanel.GetGlobalTransformWithCanvas().AffineInverse() * pointerGlobalPosition;
	// 	return true;
	// }

	/// <summary>
	/// 更新当前选中节点样式。
	/// </summary>
	public void UpdateSelectedNode(string nodeId)
	{
		selectedNodeId = nodeId;
		QueueRedraw();
	}

	// /// <summary>
	// /// 清空图快照并触发重绘。
	// /// </summary>
	// public void ClearGraph(string inputSelectedNodeId)
	// {
	// 	topologyCanvas.Clear();
	// 	selectedNodeId = inputSelectedNodeId;
	// 	QueueRedraw();
	// }



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



	public override void _Draw()
	{
		GD.Print(topologyCanvas.Nodes.Count);
		CanvasEdgePainter.DrawEdges(this, topologyCanvas);
		CanvasNodePainter.DrawNodes(this, topologyCanvas, selectedNodeId);
		drawSelectedNodeDeleteButton();
		drawDropShadow();
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




	// 识别输入事件并转发为信号。
	private void handleInputEvent(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
			{
				Vector2 graphPointerPosition = mapCanvasLocalPointerToGraph(mouseButton.Position);
				if (tryGetSelectedNodeDeleteButtonRect(out Rect2 deleteButtonRect)
					&& deleteButtonRect.HasPoint(graphPointerPosition)
					&& tryEmitNodeDeleteSignal())
				{
					return;
				}

				topologyCanvas.TryPickNodeIdAt(graphPointerPosition, out string nodeId);
				EmitSignal(SignalName.NodeSelect, nodeId, graphPointerPosition);
			}

			EmitSignal(SignalName.MouseButtonInputRecognized, mouseButton);
			return;
		}

		if (@event is InputEventMouseMotion mouseMotion)
		{
			EmitSignal(SignalName.MouseMotionInputRecognized, mouseMotion);
			return;
		}

		if (@event is InputEventKey keyEvent
			&& keyEvent.Pressed
			&& keyEvent.Keycode == Key.Delete
			&& tryEmitNodeDeleteSignal())
		{
			return;
		}
	}

	// 触发删除信号。
	private bool tryEmitNodeDeleteSignal()
	{
		if (string.IsNullOrWhiteSpace(selectedNodeId))
		{
			return false;
		}

		EmitSignal(SignalName.SelectedNodeDelete, selectedNodeId);
		return true;
	}

	// 将画布局部坐标映射到画布世界坐标。
	private Vector2 mapCanvasLocalPointerToGraph(Vector2 canvasLocalPointerPosition)
	{
		return mainViewport.CanvasTransform.AffineInverse() * canvasLocalPointerPosition;
	}




	// 计算当前选中节点的删除按钮区域。
	private bool tryGetSelectedNodeDeleteButtonRect(out Rect2 deleteButtonRect)
	{
		deleteButtonRect = default;
		if (string.IsNullOrWhiteSpace(selectedNodeId))
		{
			return false;
		}

		if (topologyCanvas.Nodes.ContainsKey(selectedNodeId) == false)
		{
			return false;
		}

		TopologyNode node = topologyCanvas.Nodes[selectedNodeId];
		Vector2 buttonPosition = new(
			node.Position.X + topologyCanvas.NodeWidth - deleteButtonSize - deleteButtonMargin,
			node.Position.Y + deleteButtonMargin);
		deleteButtonRect = new Rect2(buttonPosition, new Vector2(deleteButtonSize, deleteButtonSize));
		return true;
	}
}
