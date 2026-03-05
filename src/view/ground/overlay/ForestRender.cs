using Godot;
using System.Collections.Generic;

/// <summary>
/// 森林覆盖物渲染
/// </summary>
public partial class ForestRender : OverlayRender
{
    public override List<ComponentDefinition> GetComponentDefs(float scale)
    {
        BoxMesh mesh = new BoxMesh();
        mesh.Size = new Vector3(0.4f, 1.2f, 0.4f) * scale;
        return SingleComponent("default", mesh, Colors.DarkGreen);
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        return [ScaledInstance("default", scale, new Vector3(0, 0.6f * scale, 0))];
    }
}
