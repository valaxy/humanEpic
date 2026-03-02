using Godot;

/// <summary>
/// 地平面网格渲染类
/// 该类负责在3D场景中生成并显示地平面的辅助网格线。
/// 它通过 MeshInstance3D 和 SurfaceTool 动态生成线框，
/// 帮助用户在视觉上区分不同的地格，线条采用浅色半透明设计，避免干扰主要视觉。
/// </summary>
[GlobalClass]
public partial class GroundGridHelper : Node3D
{
    private MeshInstance3D meshInstance = null!;

    /// <summary>
    /// 初始化渲染器
    /// </summary>
    public override void _Ready()
    {
        meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
        if (meshInstance.MaterialOverride is StandardMaterial3D mat)
        {
            mat.RenderPriority = YConfig.GridRenderPriority;
        }
    }

    /// <summary>
    /// 根据给定的宽度和高度重新生成网格线
    /// </summary>
    public void UpdateGrid(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            meshInstance.Mesh = null;
            return;
        }

        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Lines);

        // 计算边界，与 DetailLayer 中的地格布局逻辑对齐
        // 每个格子中心位于：pos_x = x - width / 2.0
        float startX = -width / 2.0f - 0.5f;
        float startZ = -height / 2.0f - 0.5f;
        float endX = width / 2.0f - 0.5f;
        float endZ = height / 2.0f - 0.5f;

        // 使用 YConfig 中定义的高度
        float yOffset = YConfig.GridHelperY;

        // 绘制垂直线（沿Z轴方向延伸的线）
        for (int x = 0; x <= width; x++)
        {
            float xPos = startX + x;
            st.AddVertex(new Vector3(xPos, yOffset, startZ));
            st.AddVertex(new Vector3(xPos, yOffset, endZ));
        }

        // 绘制水平线（沿X轴方向延伸的线）
        for (int z = 0; z <= height; z++)
        {
            float zPos = startZ + z;
            st.AddVertex(new Vector3(startX, yOffset, zPos));
            st.AddVertex(new Vector3(endX, yOffset, zPos));
        }

        Mesh mesh = st.Commit();
        meshInstance.Mesh = mesh;
    }
}
