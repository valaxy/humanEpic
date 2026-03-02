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
        return new List<ComponentDefinition> { new ComponentDefinition("default", mesh, Colors.SaddleBrown) };
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        return new List<InstanceData> {
            new InstanceData {
                ComponentName = "default",
                LocalTransform = new Transform3D(Basis.FromScale(new Vector3(scale, scale, scale)), Vector3.Zero),
                VisualOffset = new Vector3(0, 0.05f * scale, 0)
            }
        };
    }
}
