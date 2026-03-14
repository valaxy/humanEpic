using Godot;
using Godot.Collections;

/// <summary>
/// 未分配池演示的拖拽投放区，用于验证拖拽载荷。
/// </summary>
[GlobalClass]
public partial class UnassignedPoolDropTarget : PanelContainer
{
	// 拖拽载荷中的节点 ID 键。
	private static readonly StringName dragNodeIdKey = "nodeId";
	// 拖拽载荷中的显示名键。
	private static readonly StringName dragDisplayTextKey = "displayText";
	// 状态文本。
	private Label statusLabel = null!;

	/// <summary>
	/// 初始化投放区状态。
	/// </summary>
	public override void _Ready()
	{
		statusLabel = GetNode<Label>("Margin/Status");
	}

	/// <summary>
	/// 判断当前拖拽载荷是否可投放。
	/// </summary>
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		return tryExtractDragPayload(data, out _, out _);
	}

	/// <summary>
	/// 接收投放并输出验证结果。
	/// </summary>
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (tryExtractDragPayload(data, out string nodeId, out string displayText) == false)
		{
			statusLabel.Text = "拖拽载荷无效。";
			GD.Print("[UnassignedPoolViewDemo] Drop payload invalid.");
			return;
		}

		statusLabel.Text = $"Drop success\nnodeId: {nodeId}\ndisplayText: {displayText}";
		GD.Print($"[UnassignedPoolViewDemo] Drop success => nodeId: {nodeId}, displayText: {displayText}");
	}

	// 解析拖拽载荷。
	private static bool tryExtractDragPayload(Variant data, out string nodeId, out string displayText)
	{
		nodeId = string.Empty;
		displayText = string.Empty;
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
		displayText = payload[dragDisplayTextKey].AsString();
		return string.IsNullOrWhiteSpace(nodeId) == false;
	}
}
