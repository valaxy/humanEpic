using Godot;

/// <summary>
/// 单个建筑的 3D 表现节点，负责非批量渲染的部分（如标签、进度条）
/// </summary>
[GlobalClass]
public partial class BuildingNode : Node3D
{
	private Sprite3D storageBarSprite = null!;
	private BuildingMesh meshRender = null!;

	private static readonly PackedScene meshScene = GD.Load<PackedScene>("res://src/view/building/BuildingMesh.tscn");

	public override void _Ready()
	{
		ensureInitialized();
	}

	private void ensureInitialized()
	{
		if (meshRender != null) return;
		setupMesh();
		setupStorageBar();
	}

	private void setupMesh()
	{
		meshRender = meshScene.Instantiate<BuildingMesh>();
		AddChild(meshRender);
	}

	private void setupStorageBar()
	{
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
		ensureInitialized();
		meshRender.UpdateText(building.Name);
		meshRender.UpdateHighlight(isSelected, false, building.Color);
		storageBarSprite.Visible = false;
	}
}
