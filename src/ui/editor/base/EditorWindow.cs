using Godot;

/// <summary>
/// 通用编辑窗口基类。
/// 提供窗口关闭信号和内容容器访问。
/// </summary>
[GlobalClass]
public partial class EditorWindow : CanvasLayer
{
	/// <summary>
	/// 当窗口请求关闭时发出。
	/// </summary>
	[Signal]
	public delegate void CloseRequestedEventHandler();

	// 关闭窗口按钮。
	private Button closeButton = null!;
	// 放置内容的容器。
	private VBoxContainer contentContainer = null!;

	/// <summary>
	/// 获取内容容器。
	/// </summary>
	public VBoxContainer ContentContainer => contentContainer;

	public override void _Ready()
	{
		closeButton = GetNode<Button>("%CloseButton");
		contentContainer = GetNode<VBoxContainer>("%ContentContainer");
		closeButton.Pressed += onClosePressed;
	}

	// 响应关闭按钮点击事件。
	private void onClosePressed()
	{
		Hide();
		EmitSignal(SignalName.CloseRequested);
	}
}
