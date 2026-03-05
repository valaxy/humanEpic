using Godot;
using System.Collections.Generic;

/// <summary>
/// 城墙覆盖物渲染
/// </summary>
public partial class WallRender : OverlayRender
{
    public override List<ComponentDefinition> GetComponentDefs(float scale)
    {
        BoxMesh nodeMesh = new BoxMesh();
        nodeMesh.Size = new Vector3(0.8f, 0.6f, 0.8f) * scale;

        BoxMesh pipeMesh = new BoxMesh();
        pipeMesh.Size = new Vector3(1.0f, 0.3f, 0.3f) * scale;

        return new List<ComponentDefinition> {
            new ComponentDefinition("node", nodeMesh, Colors.LightGray),
            new ComponentDefinition("pipe", pipeMesh, Colors.LightGray)
        };
    }

    public override List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale)
    {
        List<InstanceData> res = new List<InstanceData>();
        bool hasH = isWall(x - 1, y, ground) && isWall(x + 1, y, ground);
        bool hasV = isWall(x, y - 1, ground) && isWall(x, y + 1, ground);

        Basis scaleBasis = Basis.FromScale(new Vector3(scale, scale, scale));
        Vector3 pipeOffset = new Vector3(0, 0.3f * scale, 0);

        if (hasH)
        {
            res.Add(Instance("pipe", new Transform3D(scaleBasis, Vector3.Zero), pipeOffset));
        }

        if (hasV)
        {
            Basis basis = new Basis(Vector3.Up, Mathf.Pi / 2.0f);
            res.Add(Instance("pipe", new Transform3D(basis * scaleBasis, Vector3.Zero), pipeOffset));
        }

        if (!hasH && !hasV)
        {
            res.Add(Instance("node", new Transform3D(scaleBasis, Vector3.Zero), pipeOffset));
        }

        return res;
    }

    private bool isWall(int x, int y, Ground ground)
    {
        Grid grid = ground.GetGrid(x, y);
        return grid.OverlayType == OverlayType.Enums.WALL;
    }
}
