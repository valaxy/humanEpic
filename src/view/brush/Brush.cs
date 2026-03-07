using Godot;
using System.Linq;

/// <summary>
/// 地理编辑时的笔刷表现与逻辑节点
/// </summary>
[GlobalClass]
public partial class Brush : MeshInstance3D
{
	private const int MinBrushSize = 1;
	private const int MaxBrushSize = 10;

	private Label3D forbiddenIcon = null!;

	/// <summary>
	/// 当笔刷大小变化时发出。
	/// </summary>
	[Signal]
	public delegate void SizeChangedEventHandler(int size);

	/// <summary>
	/// 笔刷大小
	/// </summary>
	public int Size { get; private set; } = 1;

	public override void _Ready()
	{
		forbiddenIcon = GetNode<Label3D>("%ForbiddenIcon");
		BoxMesh sourceMesh = (Mesh as BoxMesh)!;
		BoxMesh instanceMesh = (BoxMesh)sourceMesh.Duplicate();
		Mesh = instanceMesh;

		StandardMaterial3D sourceMaterial = (instanceMesh.Material as StandardMaterial3D)!;
		SetSurfaceOverrideMaterial(0, (StandardMaterial3D)sourceMaterial.Duplicate());

		Visible = false;
		updateVisualSize();
	}

	/// <summary>
	/// 获取笔刷中心点影响的所有地格座标
	/// </summary>
	public Vector2I[] GetAffectedCells(int centerX, int centerY)
	{
		int offset = (Size - 1) / 2;
		return Enumerable
			.Range(-offset, Size)
			.SelectMany(dy => Enumerable.Range(-offset, Size).Select(dx => new Vector2I(centerX + dx, centerY + dy)))
			.ToArray();
	}


	/// <summary>
	/// 设置笔刷大小。
	/// </summary>
	public void SetSize(int value)
	{
		int clampedValue = Mathf.Clamp(value, MinBrushSize, MaxBrushSize);
		if (clampedValue == Size)
		{
			return;
		}

		Size = clampedValue;
		updateVisualSize();
	}



	/// <summary>
	/// 设置禁止图标可见性
	/// </summary>
	public void SetForbiddenIcon(bool visible)
	{
		forbiddenIcon.Visible = visible;
	}


	// 更新大小
	private void updateVisualSize()
	{
		BoxMesh boxMesh = (Mesh as BoxMesh)!;
		boxMesh.Size = new Vector3(Size + 0.1f, 0.2f, Size + 0.1f);
		EmitSignal(SignalName.SizeChanged, Size);
	}
}
