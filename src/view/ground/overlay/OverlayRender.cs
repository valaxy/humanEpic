using Godot;
using System.Collections.Generic;

/// <summary>
/// 覆盖物渲染抽象基类，定义覆盖物的视觉组件构成及实例化逻辑
/// </summary>
public abstract partial class OverlayRender : GodotObject
{
    /// <summary>
    /// 覆盖物视觉组件定义，包含网格和基础颜色
    /// </summary>
    public partial class ComponentDefinition : GodotObject
    {
        /// <summary>
        /// 组件的标识名称，用于在渲染器（及 `DetailLayer` 中的 MultiMesh 字典）中索引该组件。
        /// 应当在同一渲染器内部保持唯一性。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 组件使用的网格资源（Mesh）。此网格会被赋值给对应的 MultiMeshInstance 的 `Multimesh.Mesh`。
        /// </summary>
        public Mesh Mesh { get; set; }

        /// <summary>
        /// 组件的基础颜色（用于创建材质的 `AlbedoColor`）。渲染时可与顶点色或透明度结合使用。
        /// </summary>
        public Color Color { get; set; }

        public ComponentDefinition(string name, Mesh mesh, Color color)
        {
            Name = name;
            Mesh = mesh;
            Color = color;
        }
    }

    /// <summary>
    /// 覆盖物单个渲染实例的数据
    /// </summary>
    public struct InstanceData
    {
        /// <summary>
        /// 实例所对应的组件名称，必须匹配渲染器返回的某个 <see cref="ComponentDefinition"/> 的 <c>Name</c>。
        /// </summary>
        public string ComponentName;

        /// <summary>
        /// 实例在组件本地空间的变换（不包含格子世界位置偏移）。通常包含旋转与缩放信息。
        /// </summary>
        public Transform3D LocalTransform;

        /// <summary>
        /// 相对于格子中心/地表的额外位置偏移，用于将实例抬高或偏移到合适的位置。
        /// </summary>
        public Vector3 VisualOffset;
    }

    /// <summary>
    /// 获取该覆盖物所需的所有组件定义
    /// </summary>
    public abstract List<ComponentDefinition> GetComponentDefs(float scale);

    /// <summary>
    /// 获取指定地格在渲染时需要的实例数据集合
    /// </summary>
    public abstract List<InstanceData> GetCellInstances(int x, int y, Ground ground, float scale);
}
