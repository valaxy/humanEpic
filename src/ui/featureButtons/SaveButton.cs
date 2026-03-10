using Godot;

/// <summary>
/// 全局保存触发按钮。
/// </summary>
[GlobalClass]
public partial class SaveButton : EditorButton
{
	// 顶级领域模型引用。
	private GameWorld world = null!;

	// 存档初始化器。
	private readonly GameWorldInitializer gameWorldInitializer = new GameWorldInitializer();

	/// <summary>
	/// 为保存按钮注册领域模型引用。
	/// </summary>
	public void Setup(GameWorld worldData)
	{
		world = worldData;
	}

	public override void _Ready()
	{
		base._Ready();
		Pressed += onPressed;
	}

	// 响应点击并执行保存。
	private void onPressed()
	{
		gameWorldInitializer.Save(world);
	}
}