using Godot;

/// <summary>
/// 表示特定地格上正在发生的采集动作的闪烁效果
/// </summary>
public partial class GatheringEffectNode : MeshInstance3D
{
	private double timer = 0.0;
	private double duration = 1.5; // 效果持续时间，应略大于采集周期以维持连贯性
	private float flashFrequency = 12.0f; // 闪烁频率

	public override void _Ready()
	{
		// 创建一个圆柱体作为标识
		CylinderMesh mesh = new CylinderMesh();
		mesh.TopRadius = 0.55f;
		mesh.BottomRadius = 0.55f;
		mesh.Height = 0.05f;
		this.Mesh = mesh;

		StandardMaterial3D mat = new StandardMaterial3D();
		mat.AlbedoColor = new Color(1, 1, 0, 0.5f); // 半透明黄色
		mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		mat.EmissionEnabled = true;
		mat.Emission = Colors.Yellow;
		mat.EmissionEnergyMultiplier = 3.0f;
		this.MaterialOverride = mat;
	}

	/// <summary>
	/// 重置计时器，持续显示效果
	/// </summary>
	public void Reset()
	{
		timer = 0.0;
		Visible = true;
	}

	public override void _Process(double delta)
	{
		timer += delta;
		
		if (timer >= duration)
		{
			QueueFree();
			return;
		}

		// 实现基于正弦波的闪烁视觉效果
		float flashValue = Mathf.Sin((float)timer * flashFrequency);
		Visible = flashValue > 0;
	}
}
