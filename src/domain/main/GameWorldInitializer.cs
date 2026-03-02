using Godot;
using System.Text.Json;
using System.Collections.Generic;

/// <summary>
/// 负责游戏世界的初始化、保存和加载逻辑。
/// 通过 Load 方法从默认存档路径加载并恢复世界对象，通过 Save 方法将
/// </summary>
public class GameWorldInitializer
{
    private const string savePath = "res://config/map_config.json";

    /// <summary>
    /// 保存游戏状态
    /// </summary>
    public void Save(GameWorld gameWorld)
    {
        ulong start = Time.GetTicksMsec();

        string jsonString = JsonSerializer.Serialize(gameWorld.GetSaveData(), new JsonSerializerOptions { WriteIndented = false });
        using FileAccess file = FileAccess.Open(savePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"Failed to open save file at {savePath} for writing: {FileAccess.GetOpenError()}");
            return;
        }

        file.StoreString(jsonString);
        GD.Print($"Game saved successfully to {savePath}");
        GD.Print($"[Perf] GameWorld save took {Time.GetTicksMsec() - start} ms");
    }

    /// <summary>
    /// 从默认存档路径加载并恢复世界对象。
    /// </summary>
    public static GameWorld Load()
    {
        ulong start = Time.GetTicksMsec();

        Dictionary<string, object> data = JsonUtility.LoadDataFromJsonFile(savePath);
        GameWorld world = GameWorld.LoadSaveData(data);

        GD.Print($"[Perf] GameWorld init took {Time.GetTicksMsec() - start} ms");
        return world;
    }
}


// 1. 初始化模板数据
// ConstructionCostParser.Initialize();
// ProcessingParser.Initialize();
// HarvestBuildingTemplate.Initialize();
// IndustryBuildingTemplate.Initialize();
// ResidentialBuildingTemplate.Initialize();
// MarketBuildingTemplate.Initialize();
// _ = ProductTemplate.GetTemplates().Count;

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