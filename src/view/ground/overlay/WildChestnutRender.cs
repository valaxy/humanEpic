using Godot;
using System.Collections.Generic;

/// <summary>
/// 野生栗覆盖物渲染
/// </summary>
public partial class WildChestnutRender : OverlayRender
{
    public override List<ComponentDefinition> GetComponentDefs(float scale)
    {
        SphereMesh mesh = new SphereMesh();
        mesh.Radius = 0.1f * scale;
        mesh.Height = 0.2f * scale;
        return new List<ComponentDefinition> { new ComponentDefinition("nut", mesh, Color.FromHtml("#A0522D")) };
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        List<InstanceData> res = new List<InstanceData>();
        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = new Vector3(((i % 2) - 0.5f) * 0.3f * scale, 0.1f * scale, ((i / 2) - 0.5f) * 0.3f * scale);
            res.Add(new InstanceData {
                ComponentName = "nut",
                LocalTransform = new Transform3D(Basis.FromScale(new Vector3(scale, scale, scale)), Vector3.Zero),
                VisualOffset = offset
            });
        }
        return res;
    }
}
