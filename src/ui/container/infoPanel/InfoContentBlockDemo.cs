using Godot;

/// <summary>
/// InfoContentBlock 组件演示入口。
/// </summary>
[GlobalClass]
public partial class InfoContentBlockDemo : Control
{
	// 内容渲染块组件实例。
	private InfoContentBlock contentBlock = null!;
	// 渲染按钮。
	private Button renderButton = null!;
	// 清空按钮。
	private Button clearButton = null!;

	/// <summary>
	/// 初始化 Demo 按钮并默认渲染样例。
	/// </summary>
	public override void _Ready()
	{
		contentBlock = GetNode<InfoContentBlock>("%ContentBlock");
		renderButton = GetNode<Button>("%RenderButton");
		clearButton = GetNode<Button>("%ClearButton");

		renderButton.Pressed += renderDemo;
		clearButton.Pressed += () => contentBlock.Clear();

		renderDemo();
	}

	// 渲染示例数据。
	private void renderDemo()
	{
		InfoData baseInfo = new InfoData();
		baseInfo.AddText("条目", "示例A");
		baseInfo.AddNumber("数值", 42);

		InfoData progressInfo = new InfoData();
		progressInfo.AddProgress("进度", 0.68f, "68%");
		progressInfo.AddProgress("负载", 1.12f, "112%");

		InfoData payload = new InfoData();
		payload.AddGroup("基础", baseInfo);
		payload.AddGroup("指标", progressInfo);
		payload.AddText("备注", "此 demo 仅演示内容渲染块");

		contentBlock.Render(payload);
	}
}
