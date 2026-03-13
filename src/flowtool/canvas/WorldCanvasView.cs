using Godot;
using System.Linq;

/// <summary>
/// 画布世界渲染层，负责背景、节点、连线与拖拽阴影绘制。
/// </summary>
[GlobalClass]
public partial class WorldCanvasView : Node2D
{
	/// <summary>
	/// 节点选中变化信号。
	/// </summary>
	[Signal]
	public delegate void NodeSelectedEventHandler(string nodeId);

	/// <summary>
	/// 节点拖拽位移信号。
	/// </summary>
	[Signal]
	public delegate void NodeDraggedEventHandler(string nodeId, Vector2 position);

	/// <summary>
	/// 节点删除请求信号。
	/// </summary>
	[Signal]
	public delegate void DeleteRequestedEventHandler(string nodeId);

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
	// 画布领域模型。
	private WorldCanvas worldCanvas = new(5000f, 3200f, 280f, 96f);
	// 当前选中节点 ID。
	private string selectedNodeId = string.Empty;
	// 拖拽阴影节点 ID。
	private string dropShadowNodeId = string.Empty;
	// 拖拽阴影位置。
	private Vector2 dropShadowPosition = Vector2.Zero;

	/// <summary>
	/// 初始化渲染层依赖的画布领域模型。
	/// </summary>
	public void Initialize(WorldCanvas inputWorldCanvas)
	{
		worldCanvas = inputWorldCanvas;
		QueueRedraw();
	}

	/// <summary>
	/// 更新当前选中节点样式。
	/// </summary>
	public void SetSelectedNode(string nodeId)
	{
		selectedNodeId = nodeId;
		QueueRedraw();
	}

	/// <summary>
	/// 触发节点选中变化信号。
	/// </summary>
	public void EmitNodeSelected(string nodeId)
	{
		EmitSignal(SignalName.NodeSelected, nodeId);
	}

	/// <summary>
	/// 触发节点拖拽信号。
	/// </summary>
	public void EmitNodeDragged(string nodeId, Vector2 position)
	{
		EmitSignal(SignalName.NodeDragged, nodeId, position);
	}

	/// <summary>
	/// 触发删除请求信号。
	/// </summary>
	public void EmitDeleteRequested(string nodeId)
	{
		EmitSignal(SignalName.DeleteRequested, nodeId);
	}

	/// <summary>
	/// 触发落点信号。
	/// </summary>
	public void EmitNodePayloadDropped(string nodeId, Vector2 graphPosition)
	{
		EmitSignal(SignalName.NodePayloadDropped, nodeId, graphPosition);
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
		drawEdges();
		drawNodes();
		drawDropShadow();
	}

	// 绘制画布背景与边框。
	private void drawCanvasBackground()
	{
		Rect2 canvasRect = new(Vector2.Zero, worldCanvas.CanvasSize);
		DrawRect(canvasRect, canvasBackgroundColor, true);
		DrawRect(canvasRect, canvasBorderColor, false, 3f);
	}

	// 绘制所有连线。
	private void drawEdges()
	{
		worldCanvas.Edges
			.Where(edge => worldCanvas.NodeLayout.ContainsKey(edge.FromNodeId) && worldCanvas.NodeLayout.ContainsKey(edge.ToNodeId))
			.ToList()
			.ForEach(drawEdge);
	}

	// 绘制所有节点。
	private void drawNodes()
	{
		Font fallbackFont = ThemeDB.FallbackFont;
		int fallbackFontSize = ThemeDB.FallbackFontSize;
		worldCanvas.Nodes
			.Where(pair => worldCanvas.NodeLayout.ContainsKey(pair.Key))
			.Select(pair => new { pair.Key, Node = pair.Value, Position = worldCanvas.NodeLayout[pair.Key] })
			.ToList()
			.ForEach(item => drawNode(item.Key, item.Node, item.Position, fallbackFont, fallbackFontSize));
	}

	// 绘制单个节点。
	private void drawNode(string nodeId, MetricNode node, Vector2 position, Font font, int fontSize)
	{
		WorldCanvasNodePainter.Draw(this, node, position, worldCanvas.NodeSize, font, fontSize, nodeId == selectedNodeId);
	}

	// 绘制单条连线。
	private void drawEdge(MetricEdge edge)
	{
		Vector2 fromPosition = worldCanvas.NodeLayout[edge.FromNodeId];
		Vector2 toPosition = worldCanvas.NodeLayout[edge.ToNodeId];
		Vector2 fromCenter = fromPosition + (worldCanvas.NodeSize / 2f);
		Vector2 toCenter = toPosition + (worldCanvas.NodeSize / 2f);
		WorldCanvasEdgePainter.Draw(this, fromCenter, toCenter);
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
