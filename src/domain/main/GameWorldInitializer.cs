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


        hasLoadedFromDisk = true;
        GD.Print($"[Perf] GameWorldInitializer Load took {Time.GetTicksMsec() - start} ms");
        return world;
    }
}




// CountryCollection = CountryCollection.Instance;
// GameWorldDataInitializer.InitializeCountryCollection(CountryCollection);

// PopulationCollection = new PopulationCollection();
// UnitCollection = new UnitCollection(Ground, PopulationCollection, CountryCollection);

// WildlifeCollection = new WildlifeCollection();
// Buildings = new BuildingCollection(Ground, PopulationCollection, CountryCollection);
// NaturalDisasters = new NaturalDisasterCollection();

// Simulation = new Simulation(
// 	this,
// 	Ground,
// 	Buildings,
// 	WildlifeCollection,
// 	UnitCollection,
// 	NaturalDisasters,
// 	TimeSystem
// );

// bindBuildingTerritoryRefreshEvents();
// refreshBuildingTerritoryColors();