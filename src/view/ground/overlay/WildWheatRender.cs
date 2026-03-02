using Godot;
using System.Collections.Generic;

/// <summary>
/// 野生麦覆盖物渲染
/// </summary>
public partial class WildWheatRender : OverlayRender
{
    public override List<ComponentDefinition> GetComponentDefs(float scale)
    {
        CylinderMesh mesh = new CylinderMesh();
        mesh.TopRadius = 0.05f * scale;
        mesh.BottomRadius = 0.05f * scale;
        mesh.Height = 0.4f * scale;
        return new List<ComponentDefinition> { new ComponentDefinition("stalk", mesh, Color.FromHtml("#F0E68C")) };
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        List<InstanceData> res = new List<InstanceData>();
        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = new Vector3((i - 1) * 0.2f * scale, 0.2f * scale, ((i % 2) - 0.5f) * 0.2f * scale);
            res.Add(new InstanceData {
                ComponentName = "stalk",
                LocalTransform = new Transform3D(Basis.FromScale(new Vector3(scale, scale, scale)), Vector3.Zero),
                VisualOffset = offset
            });
        }
        return res;
    }
}
