using Godot;
using System;
using System.Text.Json;
using System.Collections.Generic;

/// <summary>
/// 负责游戏世界的初始化、保存和加载逻辑。
/// 通过 Load 方法从默认存档路径加载并恢复世界对象，通过 Save 方法将
/// </summary>
public class GameWorldInitializer
{
    private const string savePath = "res://config/map_config.json";
    private static bool hasLoadedFromDisk;

    /// <summary>
    /// 保存游戏状态
    /// </summary>
    public void Save(GameWorld gameWorld)
    {
        if (!hasLoadedFromDisk)
        {
            throw new InvalidOperationException("必须先调用 Load() 完成世界初始化，再执行 Save()。\n这可以防止错误地覆盖未加载的存档数据。");
        }

        ulong start = Time.GetTicksMsec();

        // 使用godot File API的原因是 res:// 路径的问题
        string jsonString = JsonSerializer.Serialize(gameWorld.GetSaveData(), new JsonSerializerOptions { WriteIndented = false });
        using FileAccess file = FileAccess.Open(savePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            throw new InvalidOperationException($"Failed to open save file at {savePath}: {FileAccess.GetOpenError()}");
        }

        file.StoreString(jsonString);
        GD.Print($"Game saved successfully to {savePath}");
        GD.Print($"[Perf] GameWorldInitializer Save took {Time.GetTicksMsec() - start} ms");
    }

    /// <summary>
    /// 从默认存档路径加载并恢复世界对象。
    /// </summary>
    public static GameWorld Load()
    {
        ulong start = Time.GetTicksMsec();

        // 1. 初始化模板数据
        // ConstructionCostParser.Initialize();
        // ProcessingParser.Initialize();
        // HarvestBuildingTemplate.Initialize();
        // IndustryBuildingTemplate.Initialize();
        // ResidentialBuildingTemplate.Initialize();
        // MarketBuildingTemplate.Initialize();
        // _ = ProductTemplate.GetTemplates().Count;

        // 2. 加载核心数据
        Dictionary<string, object> data = JsonUtility.LoadDataFromJsonFile(savePath);
        GameWorld world = GameWorld.LoadSaveData(data);
        ensureColdStart(world);

        hasLoadedFromDisk = true;
        GD.Print($"[Perf] GameWorldInitializer Load took {Time.GetTicksMsec() - start} ms");
        return world;
    }


    /// <summary>
    /// 冷启动兜底：补充一些可运行的基础数据，调试用
    /// </summary>
    private static void ensureColdStart(GameWorld gameWorld)
    {
        CountryCollection countries = gameWorld.Countries;

        if (countries.Size > 0)
        {
            return;
        }

        // 国家数据
        countries.Add(new Country("红色国家", Colors.Red));
        countries.Add(new Country("蓝色国家", Colors.DodgerBlue));

        // 单位数据
        // // 新增一组单位
        // Vector2I centerPos = new Vector2I(gameWorld.Ground.Width / 2 + 50, gameWorld.Ground.Height / 2 + 50);
        // Population population = new Population(100);
        // Country firstCountry = gameWorld.CountryCollection.GetAll()[0];
        // Unit unit = new Unit(centerPos, firstCountry, population);
        // unit.Holds.MaxCapacity = 100;
        // gameWorld.UnitCollection.AddUnit(unit);
        // GD.Print("324234");

        // // 新增另一组单位
        // Vector2I topLeftPos = new Vector2I(0, 0);
        // Population topLeftPopulation = new Population(100);
        // Country secondCountry = gameWorld.CountryCollection.GetAll().Count > 1
        // 	? gameWorld.CountryCollection.GetAll()[1]
        // 	: firstCountry;
        // Unit topLeftUnit = new Unit(topLeftPos, secondCountry, topLeftPopulation);
        // topLeftUnit.Holds.MaxCapacity = 100;
        // gameWorld.UnitCollection.AddUnit(topLeftUnit);

        GD.Print("冷启动：已生成基础数据");
    }
}