using Godot;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// 存档数据持久化管理类，负责地图、建筑、单位和时间的 JSON 序列化与加载
/// </summary>
[GlobalClass]
public partial class Persistence : RefCounted
{
	private const string SavePath = "res://config/map_config.json";

	/// <summary>
	/// 保存完整游戏状态到文件
	/// </summary>
	/// <param name="world">游戏世界对象</param>
	public static void SaveGame(GameWorld world)
	{
		if (world == null || world.Ground == null)
		{
			GD.PrintErr("Persistence Error: GameWorld or Ground is null, save aborted.");
			return;
		}

		// 1. 获取基础数据 (使用原生 Dictionary)
		Dictionary<string, object> saveData = world.Ground.GetSaveData();

		// // 2. 合并单位数据
		// if (world.UnitCollection != null)
		// {
		// 	saveData["units"] = world.UnitCollection.GetSaveData();
		// }

		// // 2.1 合并国家数据
		// if (world.CountryCollection != null)
		// {
		// 	saveData["countries"] = world.CountryCollection.GetSaveData();
		// }

		// // 3. 合并时间数据
		// if (world.TimeSystem != null)
		// {
		// 	saveData["time"] = world.TimeSystem.TotalSeconds;
		// }

		// // 4. 合并人口数据（供其他模型通过 population_id 引用）
		// if (world.PopulationCollection != null)
		// {
		// 	saveData["populations"] = world.PopulationCollection.GetSaveData();
		// }

		// // 5. 合并建筑数据
		// if (world.Buildings != null)
		// {
		// 	saveData["buildings"] = world.Buildings.GetSaveData();
		// }

		// 6. 使用 System.Text.Json 直接序列化，避免转换到 Godot Variant
		string jsonString = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = false });
		
		using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		if (file != null)
		{
			file.StoreString(jsonString);
			GD.Print($"Game saved successfully to {SavePath}");
		}
		else
		{
			GD.PrintErr($"Failed to open save file at {SavePath} for writing: {FileAccess.GetOpenError()}");
		}
	}

	/// <summary>
	/// 从磁盘加载完整游戏状态并恢复到世界对象
	/// </summary>
	/// <param name="world">游戏世界对象</param>
	public static void LoadGame(GameWorld world)
	{
		ulong start = Time.GetTicksMsec();
		
		ulong t1 = Time.GetTicksMsec();
		Dictionary<string, object> data = loadDataFromFile();
		GD.Print($"[Perf] Persistence.LoadGame: Load file and parse JSON (Native) took {Time.GetTicksMsec() - t1} ms");

		if (data.Count == 0) 
		{
			GD.Print("[Perf] Persistence.LoadGame: No data found");
			return;
		}

		loadGameFromData(data, world);
		
		GD.Print($"[Perf] Persistence.LoadGame: TOTAL load process took {Time.GetTicksMsec() - start} ms");
	}

	/// <summary>
	/// 将加载好的字典数据分发到各个领域模型中
	/// </summary>
	/// <param name="data">存档字典数据 (原生 Dictionary)</param>
	/// <param name="world">游戏世界对象</param>
	private static void loadGameFromData(Dictionary<string, object> data, GameWorld world)
	{
		if (data.Count == 0) return;

		// 1. 加载地形
		if (world.Ground != null)
		{
			ulong t = Time.GetTicksMsec();
			world.Ground.LoadData(data);
			GD.Print($"[Perf] Persistence.LoadGame: Ground.LoadData took {Time.GetTicksMsec() - t} ms");
		}

		// 2. 加载时间
		if (world.TimeSystem != null && data.ContainsKey("time"))
		{
			world.TimeSystem.TotalSeconds = System.Convert.ToSingle(data["time"]);
		}

		// 3. 加载国家集合（供单位与建筑按ID关联）
		if (world.CountryCollection != null && data.ContainsKey("countries"))
		{
			world.CountryCollection.LoadSaveData(toNativeList<Dictionary<string, object>>(data["countries"]));
		}

		// 4. 加载人口集合（供模型按ID关联）
		if (world.PopulationCollection != null && data.ContainsKey("populations"))
		{
			world.PopulationCollection.LoadSaveData(toNativeList<Dictionary<string, object>>(data["populations"]));
		}

		// 5. 加载单位（依赖国家与人口先完成加载）
		if (world.UnitCollection != null && data.ContainsKey("units"))
		{
			ulong t = Time.GetTicksMsec();
			world.UnitCollection.LoadSaveData(toNativeList<Dictionary<string, object>>(data["units"]));
			GD.Print($"[Perf] Persistence.LoadGame: UnitCollection.LoadSaveData took {Time.GetTicksMsec() - t} ms");
		}

		// 6. 加载建筑
		if (world.Buildings != null && data.ContainsKey("buildings"))
		{
			ulong t = Time.GetTicksMsec();
			object buildingsValue = data["buildings"];
			
			if (buildingsValue is Dictionary<string, object> bDict)
			{
				// 兼容历史存档（分类型分片存储）
				if (bDict.ContainsKey("residential_buildings"))
				{
					world.Buildings.LoadResidentialSaveData(toNativeList<Dictionary<string, object>>(bDict["residential_buildings"]));
				}

				if (bDict.ContainsKey("harvest_buildings"))
				{
					world.Buildings.LoadHarvestSaveData(toNativeList<Dictionary<string, object>>(bDict["harvest_buildings"]));
				}

				if (bDict.ContainsKey("production_buildings"))
				{
					world.Buildings.LoadIndustrySaveData(toNativeList<Dictionary<string, object>>(bDict["production_buildings"]));
				}

				if (bDict.ContainsKey("market_buildings"))
				{
					world.Buildings.LoadMarketSaveData(toNativeList<Dictionary<string, object>>(bDict["market_buildings"]));
				}
			}
			else if (buildingsValue is List<object> bList)
			{
				world.Buildings.LoadSaveData(bList.ConvertAll(x => (Dictionary<string, object>)x));
			}
			GD.Print($"[Perf] Persistence.LoadGame: Buildings.LoadSaveData took {Time.GetTicksMsec() - t} ms");
		}
	}

	private static List<T> toNativeList<T>(object listObj)
	{
		if (listObj is List<object> list)
		{
			List<T> result = new(list.Count);
			foreach (object item in list)
			{
				result.Add((T)item);
			}
			return result;
		}
		return new();
	}
}
