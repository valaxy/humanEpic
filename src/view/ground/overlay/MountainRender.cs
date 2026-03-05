using Godot;
using System.Collections.Generic;

/// <summary>
/// 山脉覆盖物渲染
/// </summary>
public partial class MountainRender : OverlayRender
{
	public override List<ComponentDefinition> GetComponentDefs(float scale)
	{
		PrismMesh mesh = new PrismMesh();
		mesh.LeftToRight = 0.5f;
		mesh.Size = new Vector3(0.3f * scale, 0.5f * scale, 0.3f * scale);
		return SingleComponent("default", mesh, Colors.DimGray);
	}

	public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
	{
		return [ScaledInstance("default", scale, new Vector3(0, 0.25f * scale, 0))];
	}
}
