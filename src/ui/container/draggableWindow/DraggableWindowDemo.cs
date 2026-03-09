using Godot;

/// <summary>
/// DraggableWindow 组件演示入口。
/// </summary>
[GlobalClass]
public partial class DraggableWindowDemo : Control
{
	// 可拖拽窗口实例。
	private DraggableWindow window = null!;

	// 重新显示窗口按钮。
	private Button showButton = null!;

	/// <summary>
	/// 初始化 Demo。
	/// </summary>
	public override void _Ready()
	{
		window = GetNode<DraggableWindow>("DraggableWindow");
		showButton = GetNode<Button>("ToolbarMargin/Toolbar/ShowButton");

		window.SetTitle("建筑调试窗口");
		window.CloseRequested += () => window.Visible = false;
		showButton.Pressed += showWindow;

		Label line1 = new Label();
		line1.Text = "按住标题栏拖拽，可自由移动窗口。";
		window.ContentRoot.AddChild(line1);

		Label line2 = new Label();
		line2.Text = "这是基础容器，内部可放任意业务 UI。";
		window.ContentRoot.AddChild(line2);
	}

	// 重新显示窗口并重置位置。
	private void showWindow()
	{
		window.Visible = true;
		window.Position = new Vector2(160.0f, 120.0f);
	}
}
