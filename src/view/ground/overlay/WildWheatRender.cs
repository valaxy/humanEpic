using Godot;
using System.Collections.Generic;
using System.Linq;

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
        return SingleComponent("stalk", mesh, Color.FromHtml("#F0E68C"));
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        return Indices(3)
            .Select(i =>
            {
                Vector3 offset = new Vector3((i - 1) * 0.2f * scale, 0.2f * scale, ((i % 2) - 0.5f) * 0.2f * scale);
                return ScaledInstance("stalk", scale, offset);
            })
            .ToList();
    }
}
