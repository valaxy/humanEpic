using Godot;
using Godot.Collections;

/// <summary>
/// 左侧画布 GraphEdit，负责接受拖拽激活。
/// </summary>
public partial class FlowToolCanvasGraphEdit : GraphEdit
{
	/// <summary>
	/// 节点拖入画布时触发。
	/// </summary>
	[Signal]
	public delegate void NodePayloadDroppedEventHandler(string nodeId, string nodeKind, Vector2 graphPosition);

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}

		Dictionary payload = data.AsGodotDictionary();
		return payload.ContainsKey("nodeId") && payload.ContainsKey("nodeKind");
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		Dictionary payload = data.AsGodotDictionary();
		string nodeId = payload["nodeId"].AsString();
		string nodeKind = payload["nodeKind"].AsString();
		float safeZoom = Mathf.IsZeroApprox(Zoom) ? 1f : Zoom;
		Vector2 graphPosition = (atPosition / safeZoom) + ScrollOffset;
		EmitSignal(SignalName.NodePayloadDropped, nodeId, nodeKind, graphPosition);
	}
}
