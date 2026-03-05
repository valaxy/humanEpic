using Godot;
using System.Collections.Generic;

/// <summary>
/// 桥梁覆盖物渲染
/// </summary>
public partial class BridgeRender : OverlayRender
{
    public override List<ComponentDefinition> GetComponentDefs(float scale)
    {
        BoxMesh mesh = new BoxMesh();
        mesh.Size = new Vector3(1.1f, 0.25f, 1.1f) * scale;
        return SingleComponent("default", mesh, Colors.SaddleBrown);
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        return [ScaledInstance("default", scale, new Vector3(0, 0.05f * scale, 0))];
    }
}
