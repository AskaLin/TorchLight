using Serilog;
using System.Text.Json;
using TorchLight.Statistics.Configuration;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.Mapper;

/// <summary>
/// 地圖映射器 - 負責地圖ID與名稱的轉換，以及地圖類型的判斷
/// </summary>
public class MapInfoMapper
{
    private static readonly object _lock = new();
    private static Dictionary<int, MapIdConfig> _mapIdConfig = [];
    private static readonly JsonSerializerOptions _ops = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 當地圖設定更新時觸發
    /// </summary>
    public static event Action<bool, string> OnConfigUpdated;

    /// <summary>
    /// 初始化地圖映射器（從 AppConfiguration 載入）
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            try
            {
                // 從 AppConfiguration 載入地圖ID字典
                AppConfiguration.LoadConfigData();
                _mapIdConfig = AppConfiguration.MapIdDictionary;

                Log.Information("已載入地圖設定: {MapCount} 個地圖", _mapIdConfig.Count);
                OnConfigUpdated?.Invoke(true, "地圖設定已成功載入");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "載入地圖設定失敗");
                OnConfigUpdated?.Invoke(false, $"載入失敗: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 獲取地圖資訊（透過 int mapId）
    /// </summary>
    public static MapIdConfig GetMapInfo(int mapId)
    {
        lock (_lock)
        {
            if (_mapIdConfig.TryGetValue(mapId, out var config))
            {
                return config;
            }
            return null;
        }
    }

    /// <summary>
    /// 根據地圖ID獲取地圖名稱
    /// </summary>
    public static string GetMapName(int mapId)
    {
        lock (_lock)
        {
            if (_mapIdConfig.TryGetValue(mapId, out var config))
            {
                return config.Name;
            }
            return mapId.ToString();
        }
    }

    /// <summary>
    /// 取得地圖類型
    /// </summary>
    public static MapType GetMapType(int mapId)
    {
        lock (_lock)
        {
            if (_mapIdConfig.TryGetValue(mapId, out var config))
            {
                return config.Type;
            }
            return MapType.Unknown;
        }
    }

    /// <summary>
    /// 判斷地圖類型
    /// </summary>
    public static bool CheckMapType(int mapId, MapType mapType)
    {
        lock (_lock)
        {
            if (_mapIdConfig.TryGetValue(mapId, out var config))
            {
                return config.Type == mapType;
            }
            return false;
        }
    }

    /// <summary>
    /// 新增或更新地圖映射
    /// </summary>
    public static bool AddOrUpdateMapMapping(int mapId, string mapName, MapType mapType)
    {
        lock (_lock)
        {
            try
            {
                if (_mapIdConfig.TryGetValue(mapId, out var existingConfig))
                {
                    existingConfig.Name = mapName;
                    existingConfig.Type = mapType;
                }
                else
                {
                    _mapIdConfig[mapId] = new MapIdConfig
                    {
                        Id = mapId,
                        Name = mapName,
                        Type = mapType
                    };
                }

                Log.Information("已更新地圖映射: {MapId} -> {MapName} ({MapType})", mapId, mapName, mapType);
                OnConfigUpdated?.Invoke(true, "地圖設定已更新");
                return true;
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
    public static bool DeleteMapMapping(int mapId)
    {
        lock (_lock)
        {
            try
            {
                if (_mapIdConfig.Remove(mapId))
                {
                    Log.Information("已刪除地圖映射: {MapId}", mapId);
                    OnConfigUpdated?.Invoke(true, "地圖設定已刪除");
                    return true;
                }
                return false;
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
    public static List<MapIdConfig> GetAllMapConfigs()
    {
        lock (_lock)
        {
            return [.. _mapIdConfig.Values.OrderBy(m => m.Type).ThenBy(m => m.Name)];
        }
    }

    /// <summary>
    /// 獲取所有地圖設定（按地圖類型分類）
    /// </summary>
    public static Dictionary<MapType, List<MapIdConfig>> GetAllMapConfigsByType()
    {
        lock (_lock)
        {
            return _mapIdConfig.Values
                .GroupBy(m => m.Type)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Name).ToList());
        }
    }

    /// <summary>
    /// 重新載入地圖設定
    /// </summary>
    public static void ReloadConfigs()
    {
        lock (_lock)
        {
            try
            {
                AppConfiguration.LoadConfigData();
                _mapIdConfig = AppConfiguration.MapIdDictionary;

                Log.Information("已重新載入地圖設定: {MapCount} 個地圖", _mapIdConfig.Count);
                OnConfigUpdated?.Invoke(true, "地圖設定已重新載入");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "重新載入地圖設定失敗");
                OnConfigUpdated?.Invoke(false, $"重新載入失敗: {ex.Message}");
            }
        }
    }
}
