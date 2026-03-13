using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// FlowTool 2D 画布世界渲染层，负责背景、连线与拖拽阴影绘制。
/// </summary>
[Tool]
[GlobalClass]
public partial class FlowToolCanvasWorldLayer2D : Node2D
{
	// 画布背景色。
	private static readonly Color canvasBackgroundColor = new(0.11f, 0.14f, 0.18f);
	// 画布边框色。
	private static readonly Color canvasBorderColor = new(0.24f, 0.31f, 0.39f);
	// 连线颜色。
	private static readonly Color edgeColor = new(0.46f, 0.74f, 0.95f);
	// 拖拽影子填充色。
	private static readonly Color dropShadowFillColor = new(0.78f, 0.86f, 0.98f, 0.18f);
	// 拖拽影子边框色。
	private static readonly Color dropShadowBorderColor = new(0.32f, 0.62f, 0.94f, 0.9f);

	// 画布尺寸。
	private Vector2 canvasSize = new(5000f, 3200f);
	// 节点尺寸。
	private Vector2 nodeSize = new(300f, 110f);
	// 当前节点布局映射。
	private IReadOnlyDictionary<string, Vector2> nodeLayout = new Dictionary<string, Vector2>(StringComparer.Ordinal);
	// 当前边集合。
	private IReadOnlyList<FlowToolEdge> edges = Array.Empty<FlowToolEdge>();
	// 当前拖拽阴影节点 ID。
	private string dropShadowNodeId = string.Empty;
	// 当前拖拽阴影位置。
	private Vector2 dropShadowPosition = Vector2.Zero;

	/// <summary>
	/// 设置世界尺寸与节点尺寸。
	/// </summary>
	public void ConfigureWorld(Vector2 inputCanvasSize, Vector2 inputNodeSize)
	{
		canvasSize = new Vector2(Mathf.Max(inputCanvasSize.X, 1f), Mathf.Max(inputCanvasSize.Y, 1f));
		nodeSize = new Vector2(Mathf.Max(inputNodeSize.X, 1f), Mathf.Max(inputNodeSize.Y, 1f));
		QueueRedraw();
	}

	/// <summary>
	/// 刷新当前连线渲染快照。
	/// </summary>
	public void UpdateGraph(IReadOnlyDictionary<string, Vector2> layoutByNodeId, IReadOnlyList<FlowToolEdge> activeEdges)
	{
		nodeLayout = layoutByNodeId
			.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
		edges = activeEdges.ToList();
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

	public override void _Draw()
	{
		drawCanvasBackground();
		drawEdges();
		drawDropShadow();
	}

	// 绘制画布背景与边框。
	private void drawCanvasBackground()
	{
		Rect2 canvasRect = new(Vector2.Zero, canvasSize);
		DrawRect(canvasRect, canvasBackgroundColor, true);
		DrawRect(canvasRect, canvasBorderColor, false, 3f);
	}

	// 绘制所有连线。
	private void drawEdges()
	{
		edges
			.Where(edge => nodeLayout.ContainsKey(edge.FromNodeId) && nodeLayout.ContainsKey(edge.ToNodeId))
			.ToList()
			.ForEach(drawEdge);
	}

	// 绘制单条连线（支持自环）。
	private void drawEdge(FlowToolEdge edge)
	{
		Vector2 fromPosition = nodeLayout[edge.FromNodeId];
		Vector2 toPosition = nodeLayout[edge.ToNodeId];
		Vector2 fromCenter = fromPosition + (nodeSize / 2f);
		Vector2 toCenter = toPosition + (nodeSize / 2f);

		if (edge.FromNodeId == edge.ToNodeId)
		{
			Vector2 start = fromPosition + new Vector2(nodeSize.X, nodeSize.Y * 0.5f);
			Vector2 turnRightUp = start + new Vector2(44f, -28f);
			Vector2 turnLeftUp = fromPosition + new Vector2(-44f, nodeSize.Y * 0.5f - 48f);
			Vector2 end = fromPosition + new Vector2(0f, nodeSize.Y * 0.5f);
			DrawLine(start, turnRightUp, edgeColor, 2f, true);
			DrawLine(turnRightUp, turnLeftUp, edgeColor, 2f, true);
			DrawLine(turnLeftUp, end, edgeColor, 2f, true);
			return;
		}

		DrawLine(fromCenter, toCenter, edgeColor, 2f, true);
	}

	// 绘制拖拽阴影。
	private void drawDropShadow()
	{
		if (string.IsNullOrWhiteSpace(dropShadowNodeId))
		{
			return;
		}

		Rect2 shadowRect = new(dropShadowPosition, nodeSize);
		DrawRect(shadowRect, dropShadowFillColor, true);
		DrawRect(shadowRect, dropShadowBorderColor, false, 2f);
	}
}
