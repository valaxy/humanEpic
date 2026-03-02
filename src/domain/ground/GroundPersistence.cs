using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

public partial class Ground : IPersistence<Ground>
{
    private static readonly Dictionary<SurfaceType.Enums, string> GMap = new()
    {
        { SurfaceType.Enums.GRASSLAND, "G" },
        { SurfaceType.Enums.DESERT, "D" },
        { SurfaceType.Enums.RIVER, "R" },
        { SurfaceType.Enums.SNOW, "S" }
    };

    private static readonly Dictionary<OverlayType.Enums, string> OMap = new()
    {
        { OverlayType.Enums.NONE, "." },
        { OverlayType.Enums.FOREST, "F" },
        { OverlayType.Enums.SAXAUL_TREE, "X" },
        { OverlayType.Enums.BRIDGE, "B" },
        { OverlayType.Enums.MOUNTAIN, "M" },
        { OverlayType.Enums.WALL, "W" },
        { OverlayType.Enums.ORE, "O" },
        { OverlayType.Enums.WILD_WHEAT, "T" },
        { OverlayType.Enums.WILD_CHESTNUT, "C" }
    };

    private static readonly Dictionary<char, SurfaceType.Enums> GInv = new()
    {
        { 'G', SurfaceType.Enums.GRASSLAND },
        { 'D', SurfaceType.Enums.DESERT },
        { 'R', SurfaceType.Enums.RIVER },
        { 'S', SurfaceType.Enums.SNOW }
    };

    private static readonly Dictionary<char, OverlayType.Enums> OInv = new()
    {
        { '.', OverlayType.Enums.NONE },
        { 'F', OverlayType.Enums.FOREST },
        { 'X', OverlayType.Enums.SAXAUL_TREE },
        { 'B', OverlayType.Enums.BRIDGE },
        { 'M', OverlayType.Enums.MOUNTAIN },
        { 'W', OverlayType.Enums.WALL },
        { 'O', OverlayType.Enums.ORE },
        { 'T', OverlayType.Enums.WILD_WHEAT },
        { 'C', OverlayType.Enums.WILD_CHESTNUT }
    };


    private void loadData(Dictionary<string, object> data)
    {
        if (data.ContainsKey("map"))
        {
            List<object> mapDataJson = (List<object>)data["map"];
            int h = mapDataJson.Count;
            int w = 0;
            if (h > 0)
            {
                w = ((List<object>)mapDataJson[0]).Count;
            }

            GD.Print($"[Perf] Ground.LoadData: Initializing grid array {w}x{h}...");
            ulong tLoopStart = Time.GetTicksMsec();
            mapData = new Grid[h, w];

            // 使用 Parallel.For 并行化行解析，显著提升大地图加载速度
            Parallel.For(0, h, y =>
            {
                List<object> rowJson = (List<object>)mapDataJson[y];
                for (int x = 0; x < w; x++)
                {
                    object cellJson = rowJson[x];

                    // 序列化格式：<surface><overlay>[:<amount>]，例如 "G."、"GF:100.0"
                    // 兼容旧格式：<surface><overlay><height>[:<amount>]，例如 "G.P"、"GFP:100.0"
                    if (cellJson is string s)
                    {
                        string[] parts = s.Split(':');
                        string baseData = parts[0];

                        char gChar = baseData.Length > 0 ? baseData[0] : 'G';
                        char oChar = baseData.Length > 1 ? baseData[1] : '.';

                        SurfaceType.Enums g = GInv.TryGetValue(gChar, out SurfaceType.Enums gv) ? gv : SurfaceType.Enums.GRASSLAND;
                        OverlayType.Enums o = OInv.TryGetValue(oChar, out OverlayType.Enums ov) ? ov : OverlayType.Enums.NONE;

                        float? recordAmount = null;

                        // 加载覆盖物资源量
                        if (parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
                        {
                            recordAmount = amount;
                        }

                        Overlay overlay = recordAmount.HasValue ? new Overlay(o, recordAmount.Value) : new Overlay(o);
                        Grid grid = new Grid(g, overlay);
                        mapData[y, x] = grid;
                    }
                    else
                    {
                        throw new System.Exception($"Invalid cell data at ({x}, {y}): {cellJson}");
                    }
                }
            });
            GD.Print($"[Perf] Ground.LoadData: Parallel grid loop took {Time.GetTicksMsec() - tLoopStart} ms");
        }
    }


    /// <summary>
    /// 从持久化数据字典中加载地图数据
    /// </summary>
    /// <param name="data">包含地图数据的字典</param>
    public static Ground LoadSaveData(Dictionary<string, object> data)
    {
        Ground ground = new Ground();
        ground.loadData(data);
        return ground;
    }

    /// <summary>
    /// 获取可持久化的数据字典
    /// </summary>
    /// <returns>包含地格数据的字典。map 单元格式为 &lt;surface&gt;&lt;overlay&gt;[:&lt;amount&gt;]</returns>
    public Dictionary<string, object> GetSaveData()
    {
        int h = Height;
        int w = Width;
        List<List<string>> mapJson = new List<List<string>>();
        for (int y = 0; y < h; y++)
        {
            List<string> row = new List<string>();
            for (int x = 0; x < w; x++)
            {
                Grid cell = mapData[y, x];
                string cellStr = GetVal(GMap, cell.SurfaceType, "G") +
                                 GetVal(OMap, cell.OverlayType, ".");

                // 保存覆盖物的当前资源量
                if (cell.OverlayType != OverlayType.Enums.NONE)
                {
                    cellStr += ":" + cell.Overlay.Amount.ToString("F1", CultureInfo.InvariantCulture);
                }
                row.Add(cellStr);
            }
            mapJson.Add(row);
        }

        return new Dictionary<string, object>
        {
            { "width", w },
            { "height", h },
            { "map", mapJson }
        };
    }

    private static string GetVal<T>(Dictionary<T, string> map, T key, string defaultVal) where T : notnull
    {
        return map.TryGetValue(key, out string? val) ? val : defaultVal;
    }

}