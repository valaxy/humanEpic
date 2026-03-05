using System.Collections.Generic;

/// <summary>
/// 覆盖物渲染注册表，负责管理覆盖物类型到渲染逻辑的映射
/// </summary>
public static class OverlayRenderRegistry
{
    private static readonly Dictionary<OverlayType.Enums, OverlayRender> renderers = new()
    {
        [OverlayType.Enums.FOREST] = new ForestRender(),
        [OverlayType.Enums.SAXAUL_TREE] = new SaxaulTreeRender(),
        [OverlayType.Enums.BRIDGE] = new BridgeRender(),
        [OverlayType.Enums.MOUNTAIN] = new MountainRender(),
        [OverlayType.Enums.WALL] = new WallRender(),
        [OverlayType.Enums.ORE] = new OreRender(),
        [OverlayType.Enums.WILD_WHEAT] = new WildWheatRender(),
        [OverlayType.Enums.WILD_CHESTNUT] = new WildChestnutRender()
    };

    /// <summary>
    /// 获取指定类型的渲染器
    /// </summary>
    public static OverlayRender GetRenderer(OverlayType.Enums type) => renderers[type];

    /// <summary>
    /// 获取所有已注册的渲染器类型提示
    /// </summary>
    public static IEnumerable<OverlayType.Enums> GetRegisteredTypes() => renderers.Keys;
}
