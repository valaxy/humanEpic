using System.Collections.Generic;

/// <summary>
/// 覆盖物渲染注册表，负责管理覆盖物类型到渲染逻辑的映射
/// </summary>
public static class OverlayRenderRegistry
{
    private static readonly Dictionary<OverlayType.Enums, OverlayRender> renderers = new();

    static OverlayRenderRegistry()
    {
        renderers[OverlayType.Enums.FOREST] = new ForestRender();
        renderers[OverlayType.Enums.SAXAUL_TREE] = new SaxaulTreeRender();
        renderers[OverlayType.Enums.BRIDGE] = new BridgeRender();
        renderers[OverlayType.Enums.MOUNTAIN] = new MountainRender();
        renderers[OverlayType.Enums.WALL] = new WallRender();
        renderers[OverlayType.Enums.ORE] = new OreRender();
        renderers[OverlayType.Enums.WILD_WHEAT] = new WildWheatRender();
        renderers[OverlayType.Enums.WILD_CHESTNUT] = new WildChestnutRender();
    }

    /// <summary>
    /// 获取指定类型的渲染器
    /// </summary>
    public static OverlayRender GetRenderer(OverlayType.Enums type)
    {
        return renderers[type];
    }

    /// <summary>
    /// 获取所有已注册的渲染器类型提示
    /// </summary>
    public static IEnumerable<OverlayType.Enums> GetRegisteredTypes()
    {
        return renderers.Keys;
    }
}
