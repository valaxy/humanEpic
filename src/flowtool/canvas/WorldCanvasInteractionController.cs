using Godot;
using System;
using System.Linq;

/// <summary>
/// 画布交互控制器，负责节点选择、拖拽与滚轮缩放逻辑。
/// </summary>
public sealed class WorldCanvasInteractionController
{
	// 缩放控制器。
	private readonly WorldCanvasZoomController zoomController = new();
	// 当前选中节点。
	private string selectedNodeId = string.Empty;
	// 当前拖拽节点。
	private string draggingNodeId = string.Empty;
	// 拖拽偏移量。
	private Vector2 draggingPointerOffset = Vector2.Zero;

	/// <summary>
	/// 当前选中节点 ID。
	/// </summary>
	public string SelectedNodeId => selectedNodeId;

	/// <summary>
	/// 处理鼠标按键输入，返回是否发生选中变化。
	/// </summary>
	public bool TryHandleMouseButton(InputEventMouseButton mouseButton, WorldCanvas worldCanvas, Camera2D camera, out string nextSelectedNodeId)
	{
		nextSelectedNodeId = selectedNodeId;
		if (zoomController.TryHandle(mouseButton, camera))
		{
			return false;
		}

		if (mouseButton.ButtonIndex != MouseButton.Left)
		{
			return false;
		}

		if (mouseButton.Pressed)
		{
			if (tryPickNodeIdAt(mouseButton.Position, worldCanvas, out string nodeId))
			{
				selectedNodeId = nodeId;
				draggingNodeId = nodeId;
				draggingPointerOffset = mouseButton.Position - worldCanvas.NodeLayout[nodeId];
			}
			else
			{
				selectedNodeId = string.Empty;
				draggingNodeId = string.Empty;
				draggingPointerOffset = Vector2.Zero;
			}

			nextSelectedNodeId = selectedNodeId;
			return true;
		}

		draggingNodeId = string.Empty;
		draggingPointerOffset = Vector2.Zero;
		return false;
	}

	/// <summary>
	/// 处理鼠标拖拽输入，返回是否产生节点位移。
	/// </summary>
	public bool TryHandleMouseMotion(InputEventMouseMotion mouseMotion, WorldCanvas worldCanvas, out string nodeId, out Vector2 snappedPosition)
	{
		nodeId = string.Empty;
		snappedPosition = Vector2.Zero;
		if (string.IsNullOrWhiteSpace(draggingNodeId))
		{
			return false;
		}

		if ((mouseMotion.ButtonMask & MouseButtonMask.Left) == 0)
		{
			return false;
		}

		nodeId = draggingNodeId;
		Vector2 nextPosition = mouseMotion.Position - draggingPointerOffset;
		snappedPosition = snapToCanvas(nextPosition, worldCanvas);
		return true;
	}

	/// <summary>
	/// 处理删除键请求。
	/// </summary>
	public bool TryHandleDeleteKey(InputEventKey keyEvent, out string nodeId)
	{
		nodeId = string.Empty;
		if (keyEvent.Pressed == false || keyEvent.Keycode != Key.Delete)
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(selectedNodeId))
		{
			return false;
		}

		nodeId = selectedNodeId;
		return true;
	}

	// 尝试根据坐标拾取节点。
	private static bool tryPickNodeIdAt(Vector2 canvasPosition, WorldCanvas worldCanvas, out string nodeId)
	{
		nodeId = worldCanvas.NodeLayout
			.Where(pair => new Rect2(pair.Value, worldCanvas.NodeSize).HasPoint(canvasPosition))
			.Select(static pair => pair.Key)
			.FirstOrDefault() ?? string.Empty;
		return string.IsNullOrWhiteSpace(nodeId) == false;
	}

	// 约束节点坐标到画布范围。
	private static Vector2 snapToCanvas(Vector2 position, WorldCanvas worldCanvas)
	{
		float safeX = Mathf.Clamp(position.X, 0f, worldCanvas.Width - worldCanvas.NodeWidth);
		float safeY = Mathf.Clamp(position.Y, 0f, worldCanvas.Height - worldCanvas.NodeHeight);
		return new Vector2(safeX, safeY);
	}
}
