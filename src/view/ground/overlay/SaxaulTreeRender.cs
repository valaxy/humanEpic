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
        return new List<ComponentDefinition> { new ComponentDefinition("default", mesh, Colors.OliveDrab) };
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        return new List<InstanceData>
        {
            new InstanceData
            {
                ComponentName = "default",
                LocalTransform = new Transform3D(Basis.FromScale(new Vector3(scale, scale, scale)), Vector3.Zero),
                VisualOffset = new Vector3(0, 0.45f * scale, 0)
            }
        };
    }
}
