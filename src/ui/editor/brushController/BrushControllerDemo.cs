using Godot;

/// <summary>
/// BrushController API 演示入口。
/// </summary>
[GlobalClass]
public partial class BrushControllerDemo : Control
{
	// 笔刷控制器组件。
	private BrushController brushController = null!;
	// 编辑器状态对象。
	private GroundEditor groundEditor = null!;
	// 状态显示文本。
	private Label statusLabel = null!;
	// 通过 GroundEditor 设值按钮。
	private Button setByEditorButton = null!;
	// 读取当前值按钮。
	private Button readCurrentButton = null!;

	public override void _Ready()
	{
		brushController = GetNode<BrushController>("%BrushController");
		statusLabel = GetNode<Label>("%StatusLabel");
		setByEditorButton = GetNode<Button>("%SetByEditorButton");
		readCurrentButton = GetNode<Button>("%ReadCurrentButton");

		groundEditor = new GroundEditor();
		AddChild(groundEditor);
		brushController.Setup(groundEditor);

		groundEditor.BrushSizeChanged += onGroundEditorChanged;
		setByEditorButton.Pressed += onSetByEditorPressed;
		readCurrentButton.Pressed += refreshStatus;

		refreshStatus();
	}

	// 响应 GroundEditor 值变化。
	private void onGroundEditorChanged(int size)
	{
		statusLabel.Text = $"状态变更 -> GroundEditor:{size}";
	}

	// 通过 GroundEditor 修改笔刷大小，验证反向同步到 BrushController。
	private void onSetByEditorPressed()
	{
		groundEditor.SetBrushSize(7);
		refreshStatus();
	}

	// 读取并展示当前双向绑定状态。
	private void refreshStatus()
	{
		statusLabel.Text = $"当前值 -> GroundEditor:{groundEditor.BrushSize}";
	}
}