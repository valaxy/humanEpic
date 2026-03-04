using Godot;

/// <summary>
/// 网格辅助开关按钮。
/// </summary>
[GlobalClass]
public partial class GridHelperButton : EditorButton
{
	// 被控制的网格辅助节点。
	private Node3D gridNode = null!;

	/// <summary>
	/// 绑定网格辅助节点并同步按钮状态。
	/// </summary>
	public void Setup(Node3D targetGridNode)
	{
		gridNode = targetGridNode;
		IsActive = gridNode.Visible;
	}

	public override void _Ready()
	{
		base._Ready();
		Pressed += onPressed;
	}

	// 响应按钮点击并切换网格可见性。
	private void onPressed()
	{
		bool gridVisible = !gridNode.Visible;
		gridNode.Visible = gridVisible;
		IsActive = gridVisible;
	}
}
