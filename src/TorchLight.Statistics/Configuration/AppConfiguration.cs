using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace TorchLight.Statistics.Configuration;

/// <summary>
/// 應用程式配置
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// 遊戲日誌檔案可能的路徑
    /// </summary>
    public static readonly string[] CandidateLogPaths =
    [
        @"C:\Program Files (x86)\Torchlight Infinite\Game\UE_game\TorchLight\Saved\Logs\UE_game.log",
        @"D:\Torchlight Infinite Game\UE_game\TorchLight\Saved\Logs\UE_game.log"
    ];

    /// <summary>
    /// 檔案監聽防抖動時間（毫秒）
    /// </summary>
    public const int FileWatcherDebounceMs = 500;

    /// <summary>
    /// 檔案輪詢間隔（秒）
    /// </summary>
    public const int FilePollingIntervalSeconds = 2;

    /// <summary>
    /// 是否從檔案末尾開始讀取
    /// </summary>
    public const bool StartFromFileEnd = true;

    /// <summary>
    /// 日誌時間格式
    /// </summary>
    public const string UnrealLogTimeFormat = "yyyy.MM.dd-HH.mm.ss:fff";

    /// <summary>
    /// 時區偏移（小時）- 用於轉換 UTC 到本地時間
    /// </summary>
    public const int TimeZoneOffsetHours = 8;

    /// <summary>
    /// 地圖ID對應字典
    /// </summary>
    public static Dictionary<int, MapIdConfig> MapIdDictionary { get; private set; } = [];

    /// <summary>
    /// 物品ID對應字典
    /// </summary>
    public static Dictionary<int, ItemBaseModel> ItemIdDictionary { get; private set; } = [];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// 設定項目描述
    /// </summary>
    private class ConfigDescriptor
    {
        public required string Name { get; init; }
        public required string FileName { get; init; }
        public required string SeedPath { get; init; }
        public required string ConfigPath { get; init; }
    }

    private static ConfigDescriptor MapConfig => new()
    {
        Name = "地圖",
        FileName = "MapMapper.json",
        SeedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "MapMapper.json"),
        ConfigPath = Path.Combine(AppContext.BaseDirectory, "MapMapper.json")
    };

    private static ConfigDescriptor ItemConfig => new()
    {
        Name = "物品",
        FileName = "ItemMapper.json",
        SeedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "ItemMapper.json"),
        ConfigPath = Path.Combine(AppContext.BaseDirectory, "ItemMapper.json")
    };

    public static void LoadConfigData()
    {
        // 載入地圖設定
        LoadConfig<MapItem>(MapConfig, mapItems =>
             {
                 MapIdDictionary.Clear();
                 foreach (var item in mapItems)
                 {
                     foreach (var id in item.MapIds)
                     {
                         MapIdDictionary[id] = new MapIdConfig
                         {
                             Id = id,
                             Name = item.Name,
                             Type = item.Type
                         };
                     }
                 }
                 Log.Information("已載入地圖設定: {Count} 個地圖", MapIdDictionary.Count);
             });

        // 載入物品設定
        LoadConfig<ItemBaseModel>(ItemConfig, items =>
        {
            ItemIdDictionary.Clear();
            foreach (var item in items)
            {
                ItemIdDictionary[item.Id] = item;
            }
            Log.Information("已載入物品設定: {Count} 個物品", ItemIdDictionary.Count);
        });
    }

    /// <summary>
    /// 泛型設定載入方法
    /// </summary>
    private static void LoadConfig<T>(ConfigDescriptor descriptor, Action<List<T>> loadAction)
    {
        try
        {
            // 檢查並複製種子檔案
            EnsureConfigFile(descriptor);

            // 載入 JSON
            if (!File.Exists(descriptor.ConfigPath))
            {
                Log.Error("找不到{Name}對應檔案: {Path}", descriptor.Name, descriptor.ConfigPath);
                return;
            }

            var jsonContent = File.ReadAllText(descriptor.ConfigPath);
            var items = JsonSerializer.Deserialize<List<T>>(jsonContent, _jsonOptions);

            if (items == null)
            {
                Log.Warning("無法解析{Name}對應檔案: {Path}", descriptor.Name, descriptor.ConfigPath);
                return;
            }

            loadAction(items);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "載入{Name}設定失敗: {Message}", descriptor.Name, ex.Message);
        }
    }

    /// <summary>
    /// 確保設定檔存在（不存在時從 Seed 複製）
    /// </summary>
    private static void EnsureConfigFile(ConfigDescriptor descriptor)
    {
        if (!File.Exists(descriptor.ConfigPath))
        {
            Log.Information("執行目錄下找不到 {FileName}，嘗試從 Seed 目錄複製", descriptor.FileName);

            if (File.Exists(descriptor.SeedPath))
            {
                try
                {
                    File.Copy(descriptor.SeedPath, descriptor.ConfigPath);
                    Log.Information("已從 Seed 目錄複製 {FileName} 到執行目錄", descriptor.FileName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "複製 {FileName} 失敗", descriptor.FileName);
                }
            }
            else
            {
                Log.Warning("Seed 目錄也找不到 {FileName}: {Path}", descriptor.FileName, descriptor.SeedPath);
            }
        }
    }

    /// <summary>
    /// 儲存地圖設定到 MapMapper.json
    /// </summary>
    public static bool SaveMapperToJson()
    {
        var mapperItems = MapIdDictionary.Values
            .GroupBy(m => new { m.Name, m.Type })
            .Select(g => new MapItem
            {
                MapIds = g.Select(m => m.Id).OrderBy(id => id).ToList(),
                Name = g.Key.Name,
                Type = g.Key.Type
            }).OrderBy(m => m.Type).ThenBy(m => m.Name).ToList();

        return SaveConfig(MapConfig, mapperItems);
    }

    /// <summary>
    /// 儲存物品設定到 ItemMapper.json
    /// </summary>
    public static bool SaveItemMapperToJson()
    {
        var itemList = ItemIdDictionary.Values
            .OrderBy(i => i.Type)
            .ThenBy(i => i.Id)
            .ToList();

        return SaveConfig(ItemConfig, itemList);
    }

    /// <summary>
    /// 泛型設定儲存方法
    /// </summary>
    private static bool SaveConfig<T>(ConfigDescriptor descriptor, List<T> data)
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(descriptor.ConfigPath, jsonContent);

            Log.Information("已儲存{Name}設定至: {Path}", descriptor.Name, descriptor.ConfigPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "儲存{Name}設定檔失敗", descriptor.Name);
            return false;
        }
    }

    /// <summary>
    /// 根據地圖ID取得地圖資訊
    /// </summary>
    public static MapIdConfig GetMapInfo(int mapId)
    {
        return MapIdDictionary.TryGetValue(mapId, out var mapInfo) ? mapInfo : null;
    }

    /// <summary>
    /// 根據物品ID取得物品資訊
    /// </summary>
    public static ItemBaseModel GetItemInfo(int itemId)
    {
        return ItemIdDictionary.TryGetValue(itemId, out var itemInfo) ? itemInfo : null;
    }
}
