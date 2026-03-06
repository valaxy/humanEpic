using Godot;

/// <summary>
/// 地格选中光标节点，负责选中区域的高亮显示。
/// </summary>
[GlobalClass]
public partial class GridCursor : MeshInstance3D
{
	// 可动态修改的实例材质。
	private StandardMaterial3D material = null!;

	public override void _Ready()
	{
		Mesh = (Mesh)Mesh.Duplicate();
		material = (StandardMaterial3D)GetSurfaceOverrideMaterial(0).Duplicate();
		SetSurfaceOverrideMaterial(0, material);
		Visible = false;
	}

	/// <summary>
	/// 设置光标显示尺寸。
	/// </summary>
	public void SetCursorSize(int size)
	{
		BoxMesh boxMesh = (BoxMesh)Mesh;
		boxMesh.Size = new Vector3(size + 0.1f, 0.2f, size + 0.1f);
	}

	/// <summary>
	/// 设置光标颜色。
	/// </summary>
	public void SetColor(Color color)
	{
		material.AlbedoColor = color;
		material.Emission = color;
	}

	/// <summary>
	/// 显示单个地格选中效果。
	/// </summary>
	public void ShowCell(Vector2I cellPos, Ground ground)
	{
		UpdateVisual(cellPos, new Vector2I(1, 1), ground);
	}

	/// <summary>
	/// 更新选中区域的视觉表现。
	/// </summary>
	public void UpdateVisual(Vector2I topLeft, Vector2I size, Ground ground)
	{
		SetCursorSize(size.X);
		Vector3 worldPos = ground.GridToWorld(topLeft, YConfig.SelectionY);
		worldPos.X += (size.X - 1) * 0.5f;
		worldPos.Z += (size.Y - 1) * 0.5f;
		GlobalPosition = worldPos;
		Visible = true;
	}

	/// <summary>
	/// 隐藏选中光标。
	/// </summary>
	public void Clear()
	{
		Visible = false;
	}
}
