using Godot;

/// <summary>
/// 单个建筑的 3D 表现节点
/// </summary>
[GlobalClass]
public partial class BuildingView : Node3D
{
	private Sprite3D storageBarSprite = null!;
	private BuildingMesh meshRender = null!;
	private StaticBody3D pickBody = null!;
	private Building currentBuilding = null!;

	private static readonly PackedScene meshScene = GD.Load<PackedScene>("res://src/view/building/BuildingMesh.tscn");

	/// <summary>
	/// 当建筑几何体被点击时发出。
	/// </summary>
	[Signal]
	public delegate void BuildingClickedEventHandler(Vector2I cellPos);

	public override void _Ready()
	{
		setupMesh();
		setupPickBody();
		setupStorageBar();
	}

	private void setupMesh()
	{
		meshRender = meshScene.Instantiate<BuildingMesh>();
		AddChild(meshRender);
	}

	private void setupPickBody()
	{
		// 处理与鼠标碰撞的问题
		pickBody = new StaticBody3D();
		pickBody.InputRayPickable = true;
		pickBody.InputEvent += onPickBodyInputEvent;

		CollisionShape3D collisionShape = new CollisionShape3D();
		BoxShape3D boxShape = new BoxShape3D();
		boxShape.Size = new Vector3(0.9f, 0.8f, 0.9f);
		collisionShape.Shape = boxShape;
		collisionShape.Position = new Vector3(0.0f, 0.4f, 0.0f);

		pickBody.AddChild(collisionShape);
		AddChild(pickBody);
	}

	private void setupStorageBar()
	{
		// 处理点击碰撞，可能有性能问题，后面记得优化
		storageBarSprite = new Sprite3D();
		storageBarSprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		storageBarSprite.NoDepthTest = true;
		storageBarSprite.Position = new Vector3(0, 0.55f, 0);

		SubViewport viewport = new SubViewport();
		viewport.TransparentBg = true;
		viewport.Size = new Vector2I(128, 14);
		viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		storageBarSprite.AddChild(viewport);

		// 将 Viewport 渲染的内容绑定到 Sprite3D 的纹理上
		storageBarSprite.Texture = viewport.GetTexture();

		AddChild(storageBarSprite);
		storageBarSprite.Visible = false;
	}

	/// <summary>
	/// 更新建筑的表现
	/// </summary>
	/// <param name="building">建筑实例</param>
	/// <param name="isSelected">是否被选中</param>
	public void Update(Building building, bool isSelected = false)
	{
		// TODO 考虑重命名为Setup？
		currentBuilding = building;
		meshRender.UpdateText(building.Name);
		meshRender.UpdateHighlight(isSelected, false, building.Color);
		storageBarSprite.Visible = false;
	}

	// 响应建筑几何体点击并向上抛出业务对象。
	private void onPickBodyInputEvent(Node camera, InputEvent inputEvent, Vector3 eventPosition, Vector3 normal, long shapeIdx)
	{
		if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
		{
			return;
		}

		EmitSignal(SignalName.BuildingClicked, currentBuilding.Collision.Center);
		GetViewport().SetInputAsHandled();
	}
}
