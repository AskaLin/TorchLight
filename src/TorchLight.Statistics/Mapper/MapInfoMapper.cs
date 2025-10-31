using Serilog;
using System.Text.Json;
using TorchLight.Statistics.Configuration;
using TorchLight.Statistics.Core;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.Mapper;

/// <summary>
/// 地圖映射器 - 負責地圖ID與名稱的轉換，以及地圖類型的判斷
/// </summary>
public class MapInfoMapper
{
    private static readonly object _lock = new();
    private static List<MapConfigItem> _mapConfigs = [];
    private static ConfigFileWatcher<MapConfigItem> _configWatcher;
    private static readonly JsonSerializerOptions _ops = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 設定檔路徑
    /// </summary>
    private static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "mapInfo.json");

    /// <summary>
    /// 當地圖設定更新時觸發
    /// </summary>
    public static event Action<bool, string> OnConfigUpdated;

    /// <summary>
    /// 初始化地圖映射器（從 JSON 載入）
    /// </summary>
    public static void Initialize()
    {
        LoadFromJson();

        // 初始化檔案監控器
        _configWatcher = new ConfigFileWatcher<MapConfigItem>(
          ConfigFilePath,
          LoadConfigsFromFile,
          OnConfigFileUpdated);

        _configWatcher.Initialize(_mapConfigs);
    }

    /// <summary>
    /// 從 JSON 檔案載入設定
    /// </summary>
    private static void LoadFromJson()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    Log.Warning("找不到地圖設定檔: {Path}，使用預設設定", ConfigFilePath);
                    LoadDefaultConfig();
                    SaveToJson();
                    return;
                }

                var json = File.ReadAllText(ConfigFilePath);
                var configs = JsonSerializer.Deserialize<List<MapConfigItem>>(json, _ops);

                if (configs == null || configs.Count == 0)
                {
                    Log.Error("地圖設定檔格式錯誤或為空，使用預設設定");
                    LoadDefaultConfig();
                    return;
                }

                _mapConfigs = configs;
                Log.Information("已載入地圖設定: {MapCount} 個地圖", _mapConfigs.Count);
                OnConfigUpdated?.Invoke(true, "地圖設定已成功載入");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "載入地圖設定檔失敗，使用預設設定");
                LoadDefaultConfig();
                OnConfigUpdated?.Invoke(false, $"載入失敗: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 從檔案載入設定（供 ConfigFileWatcher 使用）
    /// </summary>
    private static List<MapConfigItem> LoadConfigsFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var configs = JsonSerializer.Deserialize<List<MapConfigItem>>(json, _ops);

        if (configs == null || configs.Count == 0)
        {
            throw new InvalidOperationException("設定檔格式錯誤或為空");
        }

        lock (_lock)
        {
            _mapConfigs = configs;
        }

        return configs;
    }

    /// <summary>
    /// 設定檔更新回調
    /// </summary>
    private static void OnConfigFileUpdated(bool success, string message)
    {
        OnConfigUpdated?.Invoke(success, message);
    }

    /// <summary>
    /// 載入預設設定
    /// </summary>
    private static void LoadDefaultConfig()
    {
        _mapConfigs = [.. AppConfiguration.DefaultMapConfigs];
    }

    /// <summary>
    /// 儲存設定到 JSON 檔案
    /// </summary>
    public static bool SaveToJson()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(_mapConfigs, _ops);

                // 暫時停止檔案監控
                _configWatcher?.PauseWatching();

                File.WriteAllText(ConfigFilePath, json);

                // 恢復檔案監控
                _configWatcher?.ResumeWatching();

                Log.Information("地圖設定已儲存至: {Path}", ConfigFilePath);
                OnConfigUpdated?.Invoke(true, "地圖設定已成功儲存");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "儲存地圖設定檔失敗");
                OnConfigUpdated?.Invoke(false, $"儲存失敗: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 從完整路徑獲取地圖資訊
    /// </summary>
    public static MapInfo GetMapInfo(string fullMapPath)
    {
        lock (_lock)
        {
            var mapId = ExtractMapId(fullMapPath);
            var mapName = GetMapName(mapId);
            var mapType = GetMapType(mapId);

            return new MapInfo
            {
                Id = mapId,
                Name = mapName,
                Type = mapType,
                FullPath = fullMapPath
            };
        }
    }

    /// <summary>
    /// 從完整路徑獲取地圖名稱
    /// </summary>
    public static string GetMapNameByFullPath(string fullMapPath)
    {
        return GetMapName(ExtractMapId(fullMapPath));
    }

    /// <summary>
    /// 根據地圖ID獲取地圖名稱
    /// </summary>
    public static string GetMapName(string mapId)
    {
        lock (_lock)
        {
            var config = _mapConfigs.FirstOrDefault(m => m.Id == mapId);
            return config?.Name ?? mapId;
        }
    }

    /// <summary>
    /// 判斷地圖類型
    /// </summary>
    /// <param name="mapIdOrPath"></param>
    /// <param name="mapType"></param>
    /// <returns></returns>
    public static bool CheckMapType(string mapIdOrPath, MapType mapType)
    {
        lock (_lock)
        {
            var mapId = mapIdOrPath.Contains('/') ? ExtractMapId(mapIdOrPath) : mapIdOrPath;
            return _mapConfigs.Any(m => m.Id == mapId && m.Type == mapType);
        }
    }

    /// <summary>
    /// 取得地圖類型
    /// </summary>
    public static MapType GetMapType(string mapId)
    {
        var config = _mapConfigs.FirstOrDefault(m => m.Id == mapId);
        return config?.Type ?? MapType.Unknown;
    }

    /// <summary>
    /// 從完整路徑中提取地圖ID
    /// 例如: "Maps/GeBuLinCunLuo01/GeBuLinCunLuo01.GeBuLinCunLuo01" -> "GeBuLinCunLuo01"
    /// </summary>
    public static string ExtractMapId(string fullMapPath)
    {
        if (string.IsNullOrWhiteSpace(fullMapPath))
            return string.Empty;

        var parts = fullMapPath.Split('/');
        var lastPart = parts[^1];
        var mapId = lastPart.Split('.')[0];
        return mapId;
    }

    /// <summary>
    /// 新增或更新地圖映射
    /// </summary>
    public static bool AddOrUpdateMapMapping(string mapId, string mapName, MapType mapType)
    {
        lock (_lock)
        {
            try
            {
                var existingConfig = _mapConfigs.FirstOrDefault(m => m.Id == mapId);

                if (existingConfig != null)
                {
                    existingConfig.Name = mapName;
                    existingConfig.Type = mapType;
                }
                else
                {
                    _mapConfigs.Add(new MapConfigItem
                    {
                        Id = mapId,
                        Name = mapName,
                        Type = mapType
                    });
                }

                return SaveToJson();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "新增或更新地圖映射失敗");
                return false;
            }
        }
    }

    /// <summary>
    /// 刪除地圖映射
    /// </summary>
    public static bool DeleteMapMapping(string mapId)
    {
        lock (_lock)
        {
            try
            {
                _mapConfigs.RemoveAll(m => m.Id == mapId);
                return SaveToJson();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "刪除地圖映射失敗");
                return false;
            }
        }
    }

    /// <summary>
    /// 獲取所有地圖設定
    /// </summary>
    public static List<MapConfigItem> GetAllMapConfigs()
    {
        lock (_lock)
        {
            return [.. _mapConfigs.OrderBy(m => m.Type).ThenBy(m => m.Name)];
        }
    }

    /// <summary>
    /// 獲取所有地圖設定（按地圖類型分類）
    /// </summary>
    public static Dictionary<MapType, List<MapConfigItem>> GetAllMapConfigsByType()
    {
        lock (_lock)
        {
            return _mapConfigs
                .GroupBy(m => m.Type)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Name).ToList());
        }
    }

    /// <summary>
    /// 停止檔案監控
    /// </summary>
    public static void StopFileWatcher()
    {
        _configWatcher?.Dispose();
        _configWatcher = null;
    }
}
