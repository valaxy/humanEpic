using Godot;
using Godot.Collections;

/// <summary>
/// 画布投放覆盖层，负责接收未分配池拖拽恢复请求。
/// </summary>
[GlobalClass]
public partial class CanvasDropOverlay : Control
{
	/// <summary>
	/// 当拖拽节点投放到画布区域时发出。
	/// </summary>
	[Signal]
	public delegate void NodeRestoreRequestedEventHandler(string nodeId, Vector2 canvasLocalPointerPosition);

	// 拖拽载荷中的节点 ID 键。
	private static readonly StringName dragNodeIdKey = "nodeId";
	// 拖拽载荷中的显示名键。
	private static readonly StringName dragDisplayTextKey = "displayText";

	/// <summary>
	/// 判断当前拖拽载荷是否可投放。
	/// </summary>
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		return tryExtractNodeId(data, out _);
	}

	/// <summary>
	/// 接收投放并抛出节点恢复请求。
	/// </summary>
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (tryExtractNodeId(data, out string nodeId) == false)
		{
			return;
		}

		EmitSignal(SignalName.NodeRestoreRequested, nodeId, atPosition);
	}

	// 从拖拽载荷中解析节点 ID。
	private static bool tryExtractNodeId(Variant data, out string nodeId)
	{
		nodeId = string.Empty;
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}

		Dictionary payload = data.AsGodotDictionary();
		if (payload.ContainsKey(dragNodeIdKey) == false || payload.ContainsKey(dragDisplayTextKey) == false)
		{
			return false;
		}

		nodeId = payload[dragNodeIdKey].AsString();
		return string.IsNullOrWhiteSpace(nodeId) == false;
	}
}
