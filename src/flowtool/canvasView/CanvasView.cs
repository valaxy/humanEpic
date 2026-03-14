using Godot;
using System.Linq;

/// <summary>
/// 画布世界渲染层，负责背景、节点、连线与拖拽阴影绘制。
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
	/// 删除键输入识别信号。
	/// </summary>
	[Signal]
	public delegate void DeleteKeyInputRecognizedEventHandler();

	/// <summary>
	/// 节点拖拽落点信号。
	/// </summary>
	[Signal]
	public delegate void NodePayloadDroppedEventHandler(string nodeId, Vector2 graphPosition);

	// 画布背景色。
	private static readonly Color canvasBackgroundColor = new(0.11f, 0.14f, 0.18f);
	// 画布边框色。
	private static readonly Color canvasBorderColor = new(0.24f, 0.31f, 0.39f);
	// 拖拽阴影填充色。
	private static readonly Color dropShadowFillColor = new(0.78f, 0.86f, 0.98f, 0.18f);
	// 拖拽阴影边框色。
	private static readonly Color dropShadowBorderColor = new(0.32f, 0.62f, 0.94f, 0.9f);
	// 输入源画布容器。
	private SubViewportContainer canvasPanel = null!;
	// 画布领域模型。
	private TopologyCanvas worldCanvas = null!;
	// 当前选中节点 ID。
	private string selectedNodeId = string.Empty;
	// 拖拽阴影节点 ID。
	private string dropShadowNodeId = string.Empty;
	// 拖拽阴影位置。
	private Vector2 dropShadowPosition = Vector2.Zero;



	public override void _Ready()
	{
		canvasPanel = GetNode<SubViewportContainer>("Canvas");
		canvasPanel.GuiInput += handleInputEvent;
	}

	/// <summary>
	/// 初始化渲染层依赖的画布领域模型。
	/// </summary>
	public void Initialize(TopologyCanvas inputWorldCanvas)
	{
		worldCanvas = inputWorldCanvas;
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
	/// 更新当前选中节点样式。
	/// </summary>
	public void UpdateSelectedNode(string nodeId)
	{
		selectedNodeId = nodeId;
		QueueRedraw();
	}

	/// <summary>
	/// 触发落点信号。
	/// </summary>
	public void NotifyNodePayloadDropped(string nodeId, Vector2 graphPosition)
	{
		EmitSignal(SignalName.NodePayloadDropped, nodeId, graphPosition);
	}

	// 识别输入事件并转发为信号，不在视图内处理业务。
	private void handleInputEvent(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			EmitSignal(SignalName.MouseButtonInputRecognized, mouseButton);
			return;
		}

		if (@event is InputEventMouseMotion mouseMotion)
		{
			EmitSignal(SignalName.MouseMotionInputRecognized, mouseMotion);
			return;
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Delete)
		{
			EmitSignal(SignalName.DeleteKeyInputRecognized);
		}
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









	public override void _Draw()
	{
		drawCanvasBackground();
		CanvasEdgePainter.DrawEdges(this, worldCanvas);
		CanvasNodePainter.DrawNodes(this, worldCanvas, selectedNodeId);
		drawDropShadow();
	}

	// 绘制画布背景与边框。
	private void drawCanvasBackground()
	{
		Rect2 canvasRect = new(Vector2.Zero, worldCanvas.CanvasSize);
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

		Rect2 shadowRect = new(dropShadowPosition, worldCanvas.NodeSize);
		DrawRect(shadowRect, dropShadowFillColor, true);
		DrawRect(shadowRect, dropShadowBorderColor, false, 2f);
	}
}
