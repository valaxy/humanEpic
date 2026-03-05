using Godot;
using System.Collections.Generic;

/// <summary>
/// 梭梭树覆盖物渲染
/// </summary>
public partial class SaxaulTreeRender : OverlayRender
{
    public override List<ComponentDefinition> GetComponentDefs(float scale)
    {
        BoxMesh mesh = new BoxMesh();
        mesh.Size = new Vector3(0.36f, 0.9f, 0.36f) * scale;
        return SingleComponent("default", mesh, Colors.OliveDrab);
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        return [ScaledInstance("default", scale, new Vector3(0, 0.45f * scale, 0))];
    }
}
