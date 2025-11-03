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
    /// 新增或更新地圖映射（基於名稱）
    /// </summary>
    /// <param name="mapName">地圖名稱</param>
    /// <param name="mapIds">地圖ID列表</param>
    /// <param name="mapType">地圖類型</param>
    public static bool AddOrUpdateMapMappingByName(string mapName, List<int> mapIds, MapType mapType)
    {
        lock (_lock)
        {
            try
            {
                // 移除舊的同名地圖（但ID不在新列表中的）
                var existingIds = _mapIdConfig
                    .Where(kvp => kvp.Value.Name == mapName && !mapIds.Contains(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var oldId in existingIds)
                {
                    _mapIdConfig.Remove(oldId);
                }

                // 新增或更新所有新的ID
                foreach (var mapId in mapIds)
                {
                    _mapIdConfig[mapId] = new MapIdConfig
                    {
                        Id = mapId,
                        Name = mapName,
                        Type = mapType
                    };
                }

                // 儲存到 JSON 檔案
                if (!AppConfiguration.SaveMapperToJson())
                {
                    Log.Warning("更新記憶體成功，但儲存檔案失敗");
                }

                Log.Information("已更新地圖映射: {MapName} ({MapType}) -> [{MapIds}]",
                    mapName, mapType, string.Join(", ", mapIds));
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
    /// 刪除地圖映射（基於名稱，刪除所有相同名稱的地圖）
    /// </summary>
    public static bool DeleteMapMappingByName(string mapName)
    {
        lock (_lock)
        {
            try
            {
                var idsToRemove = _mapIdConfig.Where(kvp => kvp.Value.Name == mapName).Select(kvp => kvp.Key).ToList();

                if (idsToRemove.Count == 0)
                {
                    return false;
                }

                foreach (var id in idsToRemove)
                {
                    _mapIdConfig.Remove(id);
                }

                // 儲存到 JSON 檔案
                if (!AppConfiguration.SaveMapperToJson())
                {
                    Log.Warning("刪除記憶體成功，但儲存檔案失敗");
                }

                Log.Information("已刪除地圖映射: {MapName} ({Count} 個ID)", mapName, idsToRemove.Count);
                OnConfigUpdated?.Invoke(true, "地圖設定已刪除");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "刪除地圖映射失敗");
                return false;
            }
        }
    }

    /// <summary>
    /// 獲取所有地圖設定（按名稱分組）
    /// </summary>
    public static Dictionary<MapType, List<MapItem>> GetMapConfigsByNameGrouped()
    {
        lock (_lock)
        {
            return _mapIdConfig.Values
                .GroupBy(m => m.Type)
                .ToDictionary(
                    typeGroup => typeGroup.Key,
                    typeGroup => typeGroup.GroupBy(m => m.Name).Select(
                            nameGroup => new MapItem
                            {
                                Name = nameGroup.Key,
                                Type = nameGroup.First().Type,
                                MapIds = nameGroup.Select(m => m.Id).OrderBy(id => id).ToList()
                            }).OrderBy(g => g.Name).ToList()
                );
        }
    }
}
