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
		return new List<ComponentDefinition> { new ComponentDefinition("default", mesh, Colors.DimGray) };
	}

	public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
	{
		return new List<InstanceData> {
			new InstanceData {
				ComponentName = "default",
				LocalTransform = new Transform3D(Basis.FromScale(new Vector3(scale, scale, scale)), Vector3.Zero),
				VisualOffset = new Vector3(0, 0.25f * scale, 0)
			}
		};
	}
}
