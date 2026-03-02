using Godot;
using System.Collections.Generic;


/// <summary>
/// 细节渲染层，负责 1:1 地图精度的地格几何体及覆盖物渲染
/// </summary>
[GlobalClass]
public partial class DetailLayerNode : LayerNode
{
    private MultiMeshInstance3D groundMm = null!;
    private MultiMeshInstance3D countryMarkMm = null!;
    private Dictionary<OverlayType.Enums, Dictionary<string, MultiMeshInstance3D>> overlayMeshes = new();
    private Dictionary<MultiMeshInstance3D, StandardMaterial3D> materials = new();
    private Dictionary<OverlayType.Enums, OverlayRender> overlayRenderers = new();
    private int baseW = 0;
    private int baseH = 0;

    private static readonly Dictionary<SurfaceType.Enums, Color> surfaceBaseColors = new()
    {
        { SurfaceType.Enums.GRASSLAND, Colors.Green },
        { SurfaceType.Enums.DESERT, Colors.YellowGreen },
        { SurfaceType.Enums.RIVER, Colors.Blue },
        { SurfaceType.Enums.SNOW, Colors.White }
    };

    // 缓存 YConfig 值以避免反复跨语言访问
    private static class YConfigShort
    {
        public const float PlainY = 0.1f;
        public const float HillY = 0.4f;
        public const float MountainY = 1.2f;
        public const float TerrainBottomY = -0.1f;
    }

    public override void _Ready()
    {
        setupRenderers();
        setupMeshes();
    }

    /// <summary>
    /// 内部初始化覆盖物特定的渲染逻辑
    /// </summary>
    private void setupRenderers()
    {
        Dictionary<OverlayType.Enums, OverlayTemplate> templates = OverlayTemplate.GetTemplates();

        foreach (KeyValuePair<OverlayType.Enums, OverlayTemplate> pair in templates)
        {
            OverlayType.Enums type = pair.Key;
            if (type == OverlayType.Enums.NONE) continue;

            OverlayRender renderer = OverlayRenderRegistry.GetRenderer(type);
            overlayRenderers[type] = renderer;
        }
    }

    /// <summary>
    /// 内部初始化 MultiMesh 资源及其材质
    /// </summary>
    private void setupMeshes()
    {
        // 统一地表 MultiMesh
        groundMm = createSurfaceMm(new Vector3(1, 1.0f, 1));

        countryMarkMm = createSurfaceMm(new Vector3(1, 0.02f, 1));
        StandardMaterial3D countryMat = materials[countryMarkMm];
        Color baseColor = countryMat.AlbedoColor;
        baseColor.A = 0.25f;
        countryMat.AlbedoColor = baseColor;

        // 覆盖物 MMs
        foreach (OverlayType.Enums type in overlayRenderers.Keys)
        {
            OverlayRender renderer = overlayRenderers[type];
            Dictionary<string, MultiMeshInstance3D> components = new Dictionary<string, MultiMeshInstance3D>();

            List<OverlayRender.ComponentDefinition> componentDefs = renderer.GetComponentDefs(1.0f);
            foreach (OverlayRender.ComponentDefinition def in componentDefs)
            {
                MultiMeshInstance3D mmInst = new MultiMeshInstance3D();
                AddChild(mmInst);

                mmInst.Multimesh = new MultiMesh();
                mmInst.Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
                mmInst.Multimesh.Mesh = def.Mesh;

                StandardMaterial3D mat = new StandardMaterial3D();
                mat.AlbedoColor = def.Color;
                mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                mat.DepthDrawMode = StandardMaterial3D.DepthDrawModeEnum.Always;
                mmInst.MaterialOverride = mat;

                string name = def.Name;
                components[name] = mmInst;
                materials[mmInst] = mat;
            }
            overlayMeshes[type] = components;
        }
    }

    /// <summary>
    /// 内部助手：创建基础地表的 MultiMesh 实例
    /// </summary>
    private MultiMeshInstance3D createSurfaceMm(Vector3 size)
    {
        MultiMeshInstance3D mmInst = new MultiMeshInstance3D();
        BoxMesh mesh = new BoxMesh();
        mesh.Size = size;

        StandardMaterial3D mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(1, 1, 1, 1);
        mat.VertexColorUseAsAlbedo = true;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.DepthDrawMode = StandardMaterial3D.DepthDrawModeEnum.Always;
        mmInst.MaterialOverride = mat;
        materials[mmInst] = mat;

        mmInst.Multimesh = new MultiMesh();
        mmInst.Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        mmInst.Multimesh.UseColors = true;
        mmInst.Multimesh.Mesh = mesh;
        AddChild(mmInst);
        return mmInst;
    }

    private Color getSurfaceColor(Grid cell)
    {
        SurfaceType.Enums surface = cell.SurfaceType;

        if (!surfaceBaseColors.TryGetValue(surface, out Color baseColor))
        {
            baseColor = Colors.White;
        }

        float shade = 1.0f;

        Color shadedSurfaceColor = new Color(baseColor.R * shade, baseColor.G * shade, baseColor.B * shade, baseColor.A);
        if (cell.CountryColor.HasValue)
        {
            return shadedSurfaceColor.Lerp(cell.CountryColor.Value, 0.35f);
        }

        return shadedSurfaceColor;
    }

    private float getOverlayVisualScale(Overlay? overlay)
    {
        if (overlay == null || overlay.Type == OverlayType.Enums.NONE || overlay.Template == null || overlay.Template.MaxAmount <= 0)
        {
            return 1.0f;
        }

        float ratio = overlay.Amount / overlay.Template.MaxAmount;

        // 使用离散大小 (Discrete sizes) 为了让视觉变化更清晰，符合用户期望
        if (ratio < 0.25f) return 0.6f;
        if (ratio < 0.5f) return 0.9f;
        if (ratio < 0.8f) return 1.2f;
        return 1.5f;
    }

    /// <summary>
    /// 根据完整的地图数据更新所有实例的变换
    /// </summary>
    public override void UpdateLayer(Ground ground)
    {
        if (ground == null) return;

        ulong startTime = Time.GetTicksMsec();

        int newH = ground.Height;
        int newW = ground.Width;

        bool sizeChanged = (newH != baseH || newW != baseW);
        baseH = newH;
        baseW = newW;

        if (sizeChanged)
        {
            groundMm.Multimesh.InstanceCount = baseW * baseH;
        }

        int countryMarkCount = 0;
        for (int y = 0; y < baseH; y++)
        {
            for (int x = 0; x < baseW; x++)
            {
                Grid cell = ground.GetGrid(x, y);
                if (cell.CountryColor.HasValue)
                {
                    countryMarkCount++;
                }
            }
        }
        countryMarkMm.Multimesh.InstanceCount = countryMarkCount;

        // 覆盖物计数
        ulong countStart = Time.GetTicksMsec();
        Dictionary<OverlayType.Enums, Dictionary<string, int>> overlayCounts = new Dictionary<OverlayType.Enums, Dictionary<string, int>>();
        foreach (OverlayType.Enums type in overlayRenderers.Keys)
        {
            overlayCounts[type] = new Dictionary<string, int>();
            List<OverlayRender.ComponentDefinition> componentDefs = overlayRenderers[type].GetComponentDefs(1.0f);
            foreach (OverlayRender.ComponentDefinition def in componentDefs)
            {
                overlayCounts[type][def.Name] = 0;
            }
        }

        // 第一遍：仅针对覆盖物计数
        for (int y = 0; y < baseH; y++)
        {
            for (int x = 0; x < baseW; x++)
            {
                Grid cell = ground.GetGrid(x, y);
                if (overlayRenderers.ContainsKey(cell.OverlayType))
                {
                    float visualScale = getOverlayVisualScale(cell.Overlay);
                    List<OverlayRender.InstanceData> instances = overlayRenderers[cell.OverlayType].GetCellInstances(x, y, ground, visualScale);
                    foreach (OverlayRender.InstanceData inst in instances)
                    {
                        string compName = inst.ComponentName;
                        overlayCounts[cell.OverlayType][compName]++;
                    }
                }
            }
        }

        // 设置覆盖物 instance_count
        foreach (OverlayType.Enums type in overlayCounts.Keys)
        {
            foreach (string compName in overlayCounts[type].Keys)
            {
                overlayMeshes[type][compName].Multimesh.InstanceCount = overlayCounts[type][compName];
            }
        }

        GD.Print($"[Perf] DetailLayer: Overlay count pass took {Time.GetTicksMsec() - countStart} ms");

        // 第二遍：设置变换
        ulong transformStart = Time.GetTicksMsec();
        Dictionary<OverlayType.Enums, Dictionary<string, int>> overlayIndices = new Dictionary<OverlayType.Enums, Dictionary<string, int>>();
        foreach (OverlayType.Enums type in overlayRenderers.Keys)
        {
            overlayIndices[type] = new Dictionary<string, int>();
            List<OverlayRender.ComponentDefinition> componentDefs = overlayRenderers[type].GetComponentDefs(1.0f);
            foreach (OverlayRender.ComponentDefinition def in componentDefs)
            {
                overlayIndices[type][def.Name] = 0;
            }
        }

        for (int y = 0; y < baseH; y++)
        {
            for (int x = 0; x < baseW; x++)
            {
                Grid cell = ground.GetGrid(x, y);
                updateCellVisual(x, y, cell, ground, overlayIndices);
            }
        }

        int countryMarkIndex = 0;
        for (int y = 0; y < baseH; y++)
        {
            for (int x = 0; x < baseW; x++)
            {
                Grid cell = ground.GetGrid(x, y);
                if (!cell.CountryColor.HasValue)
                {
                    continue;
                }

                float targetY = YConfigShort.PlainY;
                Vector3 markerPos = ground.GridToWorld(new Vector2I(x, y), targetY + 0.015f);
                countryMarkMm.Multimesh.SetInstanceTransform(countryMarkIndex, new Transform3D(Basis.Identity, markerPos));
                Color markerColor = cell.CountryColor.Value;
                markerColor.A = 0.25f;
                countryMarkMm.Multimesh.SetInstanceColor(countryMarkIndex, markerColor);
                countryMarkIndex++;
            }
        }

        GD.Print($"[Perf] DetailLayer: Transform set pass took {Time.GetTicksMsec() - transformStart} ms");
        GD.Print($"[Perf] DetailLayer: Total UpdateLayer took {Time.GetTicksMsec() - startTime} ms");
    }

    /// <summary>
    /// 局部更新指定地格的视觉表现
    /// </summary>
    public override void UpdateCells(Godot.Collections.Array<Vector2I> cells, Ground ground)
    {
        bool hasSignificantChange = false;
        foreach (Vector2I pos in cells)
        {
            if (pos.X < 0 || pos.X >= baseW || pos.Y < 0 || pos.Y >= baseH) continue;

            Grid cell = ground.GetGrid(pos.X, pos.Y);
            updateGroundVisual(pos.X, pos.Y, cell, ground);
            // 覆盖物的任何变化（增加或减少）在目前基于 MultiMesh 的架构下都需要全量刷新
            hasSignificantChange = true;
        }

        if (hasSignificantChange)
        {
            UpdateLayer(ground);
        }
    }

    private void updateCellVisual(int x, int y, Grid cell, Ground ground, Dictionary<OverlayType.Enums, Dictionary<string, int>> overlayIndices)
    {
        // 更新地表
        float targetY = updateGroundVisual(x, y, cell, ground);

        // 更新覆盖物
        if (overlayRenderers.ContainsKey(cell.OverlayType))
        {
            OverlayRender renderer = overlayRenderers[cell.OverlayType];
            float visualScale = getOverlayVisualScale(cell.Overlay);
            List<OverlayRender.InstanceData> instances = renderer.GetCellInstances(x, y, ground, visualScale);
            foreach (OverlayRender.InstanceData inst in instances)
            {
                string compName = inst.ComponentName;
                MultiMeshInstance3D mm = overlayMeshes[cell.OverlayType][compName];

                Vector3 overlayPos = ground.GridToWorld(new Vector2I(x, y), targetY);
                Transform3D instTrans = inst.LocalTransform;
                Vector3 offset = inst.VisualOffset;

                Transform3D finalTrans = new Transform3D(instTrans.Basis, overlayPos + offset);
                mm.Multimesh.SetInstanceTransform(overlayIndices[cell.OverlayType][compName], finalTrans);
                overlayIndices[cell.OverlayType][compName]++;
            }
        }
    }

    private float updateGroundVisual(int x, int y, Grid cell, Ground ground)
    {
        int idx = y * baseW + x;

        float targetY = YConfigShort.PlainY;
        float bottomY = YConfigShort.TerrainBottomY;
        float height = targetY - bottomY;
        float centerY = (targetY + bottomY) / 2.0f;
        Vector3 pos = ground.GridToWorld(new Vector2I(x, y), centerY);

        Basis basis = Basis.FromScale(new Vector3(1, height, 1));
        groundMm.Multimesh.SetInstanceTransform(idx, new Transform3D(basis, pos));
        groundMm.Multimesh.SetInstanceColor(idx, getSurfaceColor(cell));

        return targetY;
    }

    /// <summary>
    /// 设置该层的透明度，用于多层平滑切换
    /// </summary>
    public override void SetAlpha(float alpha)
    {
        Visible = alpha > 0;
        foreach (StandardMaterial3D mat in materials.Values)
        {
            Color c = mat.AlbedoColor;
            if (mat == materials[countryMarkMm])
            {
                c.A = alpha * 0.25f;
            }
            else
            {
                c.A = alpha;
            }
            mat.AlbedoColor = c;
        }
    }
}
