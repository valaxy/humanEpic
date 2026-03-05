using Godot;

/// <summary>
/// BrushController API 演示入口。
/// </summary>
[GlobalClass]
public partial class BrushControllerDemo : Control
{
	// 笔刷控制器组件。
	private BrushController brushController = null!;
	// 笔刷对象。
	private Brush brush = null!;
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

		brush = new Brush();
		brushController.Setup(brush);

		brush.SizeChanged += onBrushChanged;
		setByEditorButton.Pressed += onSetByEditorPressed;
		readCurrentButton.Pressed += refreshStatus;

		refreshStatus();
	}

	// 响应 Brush 值变化。
	private void onBrushChanged(int size)
	{
		statusLabel.Text = $"状态变更 -> Brush:{size}";
	}

	// 通过 Brush 修改笔刷大小，验证反向同步到 BrushController。
	private void onSetByEditorPressed()
	{
		brush.SetSize(7);
		refreshStatus();
	}

	// 读取并展示当前双向绑定状态。
	private void refreshStatus()
	{
		statusLabel.Text = $"当前值 -> Brush:{brush.Size}";
	}
}