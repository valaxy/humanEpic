using Godot;
using System.Collections.Generic;
using System.Linq;

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
        return SingleComponent("nut", mesh, Color.FromHtml("#A0522D"));
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        return Indices(4)
            .Select(i =>
            {
                Vector3 offset = new Vector3(((i % 2) - 0.5f) * 0.3f * scale, 0.1f * scale, ((i / 2) - 0.5f) * 0.3f * scale);
                return ScaledInstance("nut", scale, offset);
            })
            .ToList();
    }
}
