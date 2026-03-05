using Godot;
using System.Collections.Generic;

/// <summary>
/// 矿石覆盖物渲染，表现为簇状结晶体
/// </summary>
public partial class OreRender : OverlayRender
{
    public override List<ComponentDefinition> GetComponentDefs(float scale)
    {
        CylinderMesh mesh = new CylinderMesh();
        mesh.TopRadius = 0.05f * scale;
        mesh.BottomRadius = 0.2f * scale;
        mesh.Height = 0.6f * scale;
        mesh.RadialSegments = 4;
        mesh.Rings = 1;
        return SingleComponent("crystal", mesh, new Color(0.6f, 0.2f, 0.8f));
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        Vector3 globalScale = new Vector3(scale, scale, scale);

        // 晶体1：中心主晶体
        Transform3D t1 = new Transform3D(Basis.FromScale(globalScale), Vector3.Zero).Rotated(new Vector3(1, 0, 1).Normalized(), Mathf.DegToRad(10));
        InstanceData i1 = Instance("crystal", t1, new Vector3(0, 0.2f * scale, 0));

        // 晶体2：侧边小晶体
        Transform3D t2 = new Transform3D(Basis.Identity, Vector3.Zero).Rotated(new Vector3(0, 0, 1), Mathf.DegToRad(45));
        InstanceData i2 = Instance("crystal", t2.Scaled(new Vector3(0.7f * scale, 0.7f * scale, 0.7f * scale)), new Vector3(0.2f * scale, 0.1f * scale, 0.1f * scale));

        // 晶体3：另一侧小晶体
        Transform3D t3 = new Transform3D(Basis.Identity, Vector3.Zero).Rotated(new Vector3(1, 0, 0), Mathf.DegToRad(-35));
        InstanceData i3 = Instance("crystal", t3.Scaled(new Vector3(0.6f * scale, 0.6f * scale, 0.6f * scale)), new Vector3(-0.15f * scale, 0.1f * scale, -0.2f * scale));

        return [i1, i2, i3];
    }
}
