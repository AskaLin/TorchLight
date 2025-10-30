using TorchLight.Statistics.Core;
using TorchLight.Statistics.Models;
using System.Text.Json;
using Serilog;

namespace TorchLight.Statistics;

/// <summary>
/// 地圖映射器 - 負責地圖ID與名稱的轉換，以及地圖類型的判斷
/// </summary>
public class MapMapper
{
    private static readonly object _lock = new();
    private static Dictionary<string, string> _mapNameMapping = new();
    private static HashSet<string> _hideoutMapIds = new();
    private static HashSet<string> _netherrealmMapIds = new();
    private static FileSystemWatcher? _fileWatcher;
    private static DateTime _lastReloadTime = DateTime.MinValue;
    private static readonly TimeSpan _reloadDebounceTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 設定檔路徑
    /// </summary>
    private static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "mapInfo.json");

    /// <summary>
    /// 當地圖設定更新時觸發
    /// </summary>
    public static event Action<bool, string>? OnConfigUpdated; // (success, message)

    /// <summary>
    /// 初始化地圖映射器（從 JSON 載入）
    /// </summary>
    public static void Initialize()
    {
        LoadFromJson();
        StartFileWatcher();
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
                    SaveToJson(); // 建立預設檔案
                    return;
                }

                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<MapInfoConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                {
                    Log.Error("地圖設定檔格式錯誤，使用預設設定");
                    LoadDefaultConfig();
                    return;
                }

                _mapNameMapping = new Dictionary<string, string>(config.MapNameMapping);
                _hideoutMapIds = new HashSet<string>(config.HideoutMapIds);
                _netherrealmMapIds = new HashSet<string>(config.NetherrealmMapIds);

                Log.Information("已載入地圖設定: {MapCount} 個地圖", _mapNameMapping.Count);
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
    /// 載入預設設定
    /// </summary>
    private static void LoadDefaultConfig()
    {
        _mapNameMapping = new()
        {
     { "XZ_YuJinZhiXiBiNanSuo200", "餘燼之息避難所" },
      { "GeBuLinCunLuo01", "隔壁林村落01" },
   { "YJ_TaiYangWangTing200", "長明宮城" },
            { "SQ_JingJiHuiTu100", "荊棘穢土" },
  { "KD_AiRenDiErCeng01", "悲鳴礦區" },
      { "DD_DiDuTingYuan000", "聖教庭院" },
            { "DD_DiDuTingYuan200", "暗夜王庭" },
          { "JH_ShengDeLanXiuDaoYuan000", "懺悔學院" },
    { "KD_RongHuoHeXin000", "熔鐵工廠" },
  { "YL_KuangReYuLin100", "微光沼澤" }
      };

        _hideoutMapIds = new()
 {
            "XZ_YuJinZhiXiBiNanSuo200"
      };

        _netherrealmMapIds = new()
    {
            "GeBuLinCunLuo01",
  "YJ_TaiYangWangTing200",
      "SQ_JingJiHuiTu100",
       "KD_AiRenDiErCeng01",
   "DD_DiDuTingYuan000",
   "JH_ShengDeLanXiuDaoYuan000",
            "KD_RongHuoHeXin000",
    "DD_DiDuTingYuan200",
     "YL_KuangReYuLin100"
  };
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
                var config = new MapInfoConfig
                {
                    MapNameMapping = new Dictionary<string, string>(_mapNameMapping),
                    HideoutMapIds = new List<string>(_hideoutMapIds),
                    NetherrealmMapIds = new List<string>(_netherrealmMapIds)
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                // 暫時停止檔案監控
                var wasWatching = _fileWatcher?.EnableRaisingEvents ?? false;
                if (_fileWatcher != null)
                    _fileWatcher.EnableRaisingEvents = false;

                File.WriteAllText(ConfigFilePath, json);

                // 恢復檔案監控
                if (_fileWatcher != null && wasWatching)
                    _fileWatcher.EnableRaisingEvents = true;

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
    /// 啟動檔案監控
    /// </summary>
    private static void StartFileWatcher()
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            var fileName = Path.GetFileName(ConfigFilePath);

            if (string.IsNullOrEmpty(directory))
                return;

            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnConfigFileChanged;
            Log.Information("已啟動地圖設定檔監控: {Path}", ConfigFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "啟動檔案監控失敗");
        }
    }

    /// <summary>
    /// 檔案變更事件處理
    /// </summary>
    private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // 防抖動：避免短時間內重複載入
        var now = DateTime.Now;
        if ((now - _lastReloadTime) < _reloadDebounceTime)
            return;

        _lastReloadTime = now;

        // 延遲一小段時間，確保檔案寫入完成
        Task.Delay(500).ContinueWith(_ =>
           {
               Log.Information("偵測到地圖設定檔變更，重新載入...");

               // 備份當前設定
               var backupMapping = new Dictionary<string, string>(_mapNameMapping);
               var backupHideout = new HashSet<string>(_hideoutMapIds);
               var backupNetherrealm = new HashSet<string>(_netherrealmMapIds);

               try
               {
                   LoadFromJson();
               }
               catch (Exception ex)
               {
                   Log.Error(ex, "重新載入地圖設定失敗，恢復原設定");

                   // 恢復備份
                   lock (_lock)
                   {
                       _mapNameMapping = backupMapping;
                       _hideoutMapIds = backupHideout;
                       _netherrealmMapIds = backupNetherrealm;
                   }

                   OnConfigUpdated?.Invoke(false, $"設定檔更新失敗: {ex.Message}");
               }
           });
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
            var mapType = DetermineMapType(mapId);

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
            return _mapNameMapping.TryGetValue(mapId, out var name) ? name : mapId;
        }
    }

    /// <summary>
    /// 判斷是否為藏身處地圖
    /// </summary>
    public static bool IsHideoutMap(string mapIdOrPath)
    {
        lock (_lock)
        {
            var mapId = mapIdOrPath.Contains('/') ? ExtractMapId(mapIdOrPath) : mapIdOrPath;
            return _hideoutMapIds.Contains(mapId);
        }
    }

    /// <summary>
    /// 判斷是否為異界地圖
    /// </summary>
    public static bool IsNetherrealmMap(string mapIdOrPath)
    {
        lock (_lock)
        {
            var mapId = mapIdOrPath.Contains('/') ? ExtractMapId(mapIdOrPath) : mapIdOrPath;
            return _netherrealmMapIds.Contains(mapId);
        }
    }

    /// <summary>
    /// 判斷地圖類型
    /// </summary>
    private static MapType DetermineMapType(string mapId)
    {
        if (_hideoutMapIds.Contains(mapId))
            return MapType.Hideout;

        if (_netherrealmMapIds.Contains(mapId))
            return MapType.Netherrealm;

        return MapType.Unknown;
    }

    /// <summary>
    /// 從完整路徑中提取地圖ID
    /// 例如: "Maps/GeBuLinCunLuo01/GeBuLinCunLuo01.GeBuLinCunLuo01" -> "GeBuLinCunLuo01"
    /// </summary>
    private static string ExtractMapId(string fullMapPath)
    {
        if (string.IsNullOrWhiteSpace(fullMapPath))
            return string.Empty;

        var parts = fullMapPath.Split('/');
        var lastPart = parts[^1]; // 取得最後一部分
        var mapId = lastPart.Split('.')[0]; // 去除副檔名
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
                // 更新名稱映射
                _mapNameMapping[mapId] = mapName;

                // 先從所有集合中移除
                _hideoutMapIds.Remove(mapId);
                _netherrealmMapIds.Remove(mapId);

                // 根據類型加入對應集合
                switch (mapType)
                {
                    case MapType.Hideout:
                        _hideoutMapIds.Add(mapId);
                        break;
                    case MapType.Netherrealm:
                        _netherrealmMapIds.Add(mapId);
                        break;
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
                _mapNameMapping.Remove(mapId);
                _hideoutMapIds.Remove(mapId);
                _netherrealmMapIds.Remove(mapId);

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
            var result = new List<MapConfigItem>();

            foreach (var kvp in _mapNameMapping)
            {
                var mapType = "Unknown";
                if (_hideoutMapIds.Contains(kvp.Key))
                    mapType = "Hideout";
                else if (_netherrealmMapIds.Contains(kvp.Key))
                    mapType = "Netherrealm";

                result.Add(new MapConfigItem
                {
                    MapId = kvp.Key,
                    MapName = kvp.Value,
                    MapType = mapType
                });
            }

            return result.OrderBy(m => m.MapType).ThenBy(m => m.MapName).ToList();
        }
    }

    /// <summary>
    /// 停止檔案監控
    /// </summary>
    public static void StopFileWatcher()
    {
        _fileWatcher?.Dispose();
        _fileWatcher = null;
    }
}
