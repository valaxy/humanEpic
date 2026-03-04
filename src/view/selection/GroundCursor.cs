using Godot;

/// <summary>
/// 地面光标节点，用于选中高亮
/// </summary>
[GlobalClass]
public partial class GroundCursor : MeshInstance3D
{
	private StandardMaterial3D? material;

	public override void _Ready()
	{
		if (Mesh != null)
		{
			Mesh = (Mesh)Mesh.Duplicate();
		}
		
		// 允许动态修改材质（例如改变选中颜色）
		if (GetSurfaceOverrideMaterial(0) is StandardMaterial3D mat)
		{
			material = (StandardMaterial3D)mat.Duplicate();
			SetSurfaceOverrideMaterial(0, material);
		}
		
		Visible = false;
	}

	/// <summary>
	/// 设置选中框大小
	/// </summary>
	public void SetCursorSize(int size)
	{
		if (Mesh is BoxMesh boxMesh)
		{
			boxMesh.Size = new Vector3(size + 0.1f, 0.2f, size + 0.1f);
		}
	}

	/// <summary>
	/// 设置光标是否可见
	/// </summary>
	public void SetCursorVisible(bool visible)
	{
		Visible = visible;
	}

	/// <summary>
	/// 设置材质颜色
	/// </summary>
	public void SetColor(Color color)
	{
		if (material != null)
		{
			material.AlbedoColor = color;
			material.Emission = color;
		}
	}

	/// <summary>
	/// 显示地格选中视觉效果
	/// </summary>
	public void ShowCell(Vector2I pos, Ground ground)
	{
		UpdateVisual(pos, new Vector2I(1, 1), ground);
	}

	// TOOD ShowBuilding违反了高内聚原则？
	/// <summary>
	/// 显示建筑选中视觉效果
	/// </summary>
	public void ShowBuilding(Vector2I topLeft, Vector2I size, Ground ground)
	{
		UpdateVisual(topLeft, size, ground);
	}

	/// <summary>
	/// 更新视觉表现（位置和大小）
	/// </summary>
	public void UpdateVisual(Vector2I pos, Vector2I size, Ground ground)
	{
		SetCursorSize(size.X);
		Vector3 worldPos = ground.GridToWorld(pos, YConfig.SelectionY);

		// 根据大小偏移到中心
		worldPos.X += (size.X - 1) * 0.5f;
		worldPos.Z += (size.Y - 1) * 0.5f;

		GlobalPosition = worldPos;
		Visible = true;
	}

	/// <summary>
	/// 隐藏视觉效果
	/// </summary>
	public void Clear()
	{
		Visible = false;
	}
}
