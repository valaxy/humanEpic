using Godot;
using System.Collections.Generic;

/// <summary>
/// 战略层渲染器，负责在宏观缩放级别下显示覆盖物区域
/// </summary>
[GlobalClass]
public partial class StrategyLayerNode : LayerNode
{
    private Dictionary<OverlayType.Enums, MultiMeshInstance3D> overlayMeshes = new();
    private Dictionary<MultiMeshInstance3D, StandardMaterial3D> materials = new();
    private float alphaVal = 0.0f;

    public override void _Ready()
    {
        setupMeshes();
    }

    // 初始化各类型的 MultiMesh
    private void setupMeshes()
    {
        Dictionary<OverlayType.Enums, OverlayTemplate> templates = OverlayTemplate.GetTemplates();
        foreach (KeyValuePair<OverlayType.Enums, OverlayTemplate> pair in templates)
        {
            OverlayType.Enums type = pair.Key;
            if (type == OverlayType.Enums.NONE)
            {
                continue;
            }

            OverlayTemplate template = pair.Value;
            MultiMeshInstance3D mmInst = new MultiMeshInstance3D();
            mmInst.Name = "Strategy_Overlay_" + type.ToString();
            AddChild(mmInst);

            MultiMesh mm = new MultiMesh();
            mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;

            // 使用平坦的薄方块作为区域显示
            BoxMesh mesh = new BoxMesh();
            mesh.Size = new Vector3(1.0f, 0.4f, 1.0f);
            mm.Mesh = mesh;
            mmInst.Multimesh = mm;

            StandardMaterial3D mat = new StandardMaterial3D();
            mat.AlbedoColor = template.Color;
            Color color = mat.AlbedoColor;
            color.A = 0.5f;
            mat.AlbedoColor = color;
            mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            mat.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded; // 纯色渲染
            // 确保战略层渲染在细节层之上，或者在特定层级
            mat.RenderPriority = YConfig.StrategyLayerRenderPriority;
            mmInst.MaterialOverride = mat;

            overlayMeshes[type] = mmInst;
            materials[mmInst] = mat;
        }

        this.Visible = false;
    }

    /// <summary>
    /// 设置图层整体透明度
    /// </summary>
    /// <param name="alpha">透明度值</param>
    public override void SetAlpha(float alpha)
    {
        this.alphaVal = alpha;
        this.Visible = alpha > 0.01f;
        foreach (MultiMeshInstance3D mmInst in materials.Keys)
        {
            StandardMaterial3D mat = materials[mmInst];
            Color color = mat.AlbedoColor;
            color.A = alpha * 0.7f;
            mat.AlbedoColor = color;
        }
    }

    /// <summary>
    /// 根据地图数据更新战略层渲染
    /// </summary>
    /// <param name="ground">地形数据模型</param>
    public override void UpdateLayer(Ground ground)
    {
        if (ground == null || !Visible)
        {
            return;
        }

        int w = ground.Width;
        int h = ground.Height;
        if (w == 0 || h == 0)
        {
            return;
        }

        Dictionary<OverlayType.Enums, List<Vector2I>> overlayPositions = new();

        // 遍历所有格子，统计各类型的覆盖物位置
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Grid grid = ground.GetGrid(x, y);
                if (grid != null && grid.OverlayType != OverlayType.Enums.NONE)
                {
                    OverlayType.Enums type = grid.OverlayType;
                    if (!overlayPositions.ContainsKey(type))
                    {
                        overlayPositions[type] = new List<Vector2I>();
                    }
                    overlayPositions[type].Add(new Vector2I(x, y));
                }
            }
        }

        // 更新 MultiMesh 实例
        foreach (OverlayType.Enums type in overlayMeshes.Keys)
        {
            MultiMeshInstance3D mmInst = overlayMeshes[type];
            MultiMesh mm = mmInst.Multimesh;

            List<Vector2I> positions = overlayPositions.ContainsKey(type) ? overlayPositions[type] : new List<Vector2I>();

            mm.InstanceCount = positions.Count;
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2I p = positions[i];
                Transform3D t = new Transform3D();
                // 将中心点对齐到世界坐标
                t.Origin = ground.GridToWorld(p, 0.5f); // 稍微抬高一点
                mm.SetInstanceTransform(i, t);
            }
        }
    }
}
