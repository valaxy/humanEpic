using Godot;

/// <summary>
/// 地图上建筑物，具有建造状态与功能组件。
/// </summary>
[Persistable]
[PersistEntity(typeof(BuildingCollection))]
public class Building : IIdModel, IInfo
{
    // 建筑模板缓存
    private BuildingTemplate template = default!;


    // 建筑自增 ID。
    [PersistField]
    private static int nextId = 1;

    // 建筑唯一标识。
    [PersistField]
    private int id;

    // 所属国家。
    [PersistField]
    private Country country = null!;

    // 碰撞信息（总是占用 1x1 地格）。
    [PersistField]
    private AtomCollision collision = null!;

    // 民宅居住组件。
    [PersistField]
    private Residential? residential;

    // 市场功能组件。
    [PersistField]
    private BuildingMarket? market;

    /// <summary>
    /// 建筑类型枚举值，来自模板。
    /// </summary>
    public BuildingType.Enums TypeId
    {
        get => template.TypeId;
        private set => template = BuildingTemplate.GetTemplate(value);
    }



    /// <summary>
    /// 名称。
    /// </summary>
    public string Name => template.Name;

    /// <summary>
    /// 建筑渲染颜色。
    /// </summary>
    public Color Color => template.Color;

    /// <summary>
    /// 建筑唯一标识。
    /// </summary>
    public int Id => id;

    /// <summary>
    /// 所属国家。
    /// </summary>
    public Country Country => country;

    /// <summary>
    /// 碰撞信息（总是占用 1x1 地格）。
    /// </summary>
    public AtomCollision Collision => collision;

    /// <summary>
    /// 民宅居住组件，可以为空。
    /// </summary>
    public Residential? Residential => residential;

    /// <summary>
    /// 市场功能组件，可以为空。
    /// </summary>
    public BuildingMarket? Market => market;

    /// <summary>
    /// 无参构造函数，供反持久化调用。
    /// </summary>
    private Building()
    {
    }

    /// <summary>
    /// 初始化建筑。
    /// </summary>
    public Building(BuildingTemplate template, Vector2I pos, Country country)
    {
        id = nextId++;
        this.template = template;
        this.country = country;
        collision = new AtomCollision(pos);
        residential = ResidentialTemplate.HasTemplate(template.TypeId)
            ? new Residential(ResidentialTemplate.GetTemplate(template.TypeId).MaxPopulation)
            : null;
        market = template.TypeId == BuildingType.Enums.Market
            ? new BuildingMarket()
            : null;
    }

    /// <summary>
    /// 获取用于 UI 展示的建筑信息。
    /// </summary>
    public virtual InfoData GetInfoData()
    {
        InfoData basicInfoNode = new();
        basicInfoNode.AddText("名称", Name);
        basicInfoNode.AddText("所属国家", Country.Name);

        InfoData data = new();
        data.AddGroup("基本信息", basicInfoNode);

        if (Residential != null)
        {
            data.AddGroup("居住信息", Residential.GetInfoData());
        }

        if (Market != null)
        {
            data.AddGroup("市场信息", Market.GetInfoData());
        }

        return data;
    }
}
