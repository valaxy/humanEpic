using Godot;

/// <summary>
/// SidePanel 组件演示入口。
/// </summary>
[GlobalClass]
public partial class SidePanelDemo : Control
{
	// 侧边栏实例。
	private SidePanel sidePanel = null!;

	/// <summary>
	/// 初始化 Demo。
	/// </summary>
	public override void _Ready()
	{
		sidePanel = GetNode<SidePanel>("SidePanel");
		sidePanel.SetTitle("SidePanel Demo");
		sidePanel.CloseRequested += () => sidePanel.Visible = false;

		VBoxContainer contentRoot = sidePanel.ContentRoot;
		Label tip = new Label();
		tip.Text = "这是一个与业务内容无关的通用侧边栏容器。";
		contentRoot.AddChild(tip);

		Label feature = new Label();
		feature.Text = "你可以在 ContentRoot 下自由插入任意控件。";
		contentRoot.AddChild(feature);
	}
}
