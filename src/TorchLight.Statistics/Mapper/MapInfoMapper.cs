using TorchLight.Statistics.Core;
using TorchLight.Statistics.Models;
using System.Text.Json;
using Serilog;
using TorchLight.Statistics.Enums;

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
    /// 預設地圖設定
    /// </summary>
    private static readonly List<MapConfigItem> DefaultMapConfigs =
    [
        new() { Id = "XZ_YuJinZhiXiBiNanSuo200", Name = "餘燼之息", Type = MapType.Hideout },
        new() { Id = "GeBuLinCunLuo01", Name = "災厄之林", Type = MapType.Netherrealm },
        new() { Id = "YJ_TaiYangWangTing200", Name = "長明宮城", Type = MapType.Netherrealm },
        new() { Id = "SQ_JingJiHuiTu100", Name = "荊棘穢土", Type = MapType.Netherrealm },
        new() { Id = "KD_AiRenDiErCeng01", Name = "悲鳴礦區", Type = MapType.Netherrealm },
        new() { Id = "DD_DiDuTingYuan000", Name = "聖教庭院", Type = MapType.Netherrealm },
        new() { Id = "DD_DiDuTingYuan200", Name = "暗夜王庭", Type = MapType.Netherrealm },
        new() { Id = "JH_ShengDeLanXiuDaoYuan000", Name = "懺悔學院", Type = MapType.Netherrealm },
        new() { Id = "KD_RongHuoHeXin000", Name = "熔鐵工廠", Type = MapType.Netherrealm },
        new() { Id = "YL_KuangReYuLin100", Name = "微光沼澤", Type = MapType.Netherrealm },
        new() { Id = "SD_ShouGuSiDi000", Name = "龍眠峽谷", Type = MapType.Netherrealm },
        new() { Id = "BZ_NaGouZhiXi100", Name = "汙穢王座", Type = MapType.SecretRealm },
        new() { Id = "KD_AiRenDiSanCeng", Name = "群山之心", Type = MapType.Netherrealm },
        new() { Id = "SQ_BianChuiZhiDi200", Name = "蠻荒原野", Type = MapType.Netherrealm },
        new() { Id = "SD_ShouGuLinDi000", Name = "曲折谷地", Type = MapType.Netherrealm },
        new() { Id = "YJ_LuoRiQiongDi200", Name = "落日穹底", Type = MapType.Netherrealm },
        new() { Id = "KD_YuanSuKuangDong000", Name = "元素礦洞", Type = MapType.Netherrealm },
        new() { Id = "YanYuZhiGu", Name = "炎獄之谷", Type = MapType.Boss },
        new() { Id = "YL_BeiFengLinDi201", Name = "悲風林地", Type = MapType.Netherrealm },
        new() { Id = "SD_GeBuLinShanZhai", Name = "暗影前哨", Type = MapType.Netherrealm },
        new() { Id = "KD_AiRenKuangDong01", Name = "荒棄礦場", Type = MapType.Netherrealm },
        new() { Id = "YL_XiDiChongGu200", Name = "母巢密林", Type = MapType.Netherrealm },
        new() { Id = "DD_QunLangJieXiang200", Name = "幽暗街巷", Type = MapType.Netherrealm },
        new() { Id = "JH_ShenHeJuSuo000", Name = "流光神座", Type = MapType.Netherrealm },
        new() { Id = "JH_YiWangMiDian000", Name = "苦痛秘殿", Type = MapType.Netherrealm },
        new() { Id = "DD_ShengTingZhuangYuan000", Name = "常世宮闈", Type = MapType.Netherrealm },
        new() { Id = "YL_MaNeiLaYuLin100", Name = "汙濁叢林", Type = MapType.Netherrealm },
        new() { Id = "YJ_ShuXiDaTing200", Name = "鏡中禮堂", Type = MapType.Netherrealm },
        new() { Id = "SQ_EWuHuangCun100", Name = "惡武荒村", Type = MapType.Netherrealm },
        new() { Id = "KD_CangBaoDongKu000", Name = "乾涸礦場", Type = MapType.Netherrealm },
        new() { Id = "DD_TanXiZhiQiang000", Name = "悲歌之牆", Type = MapType.Netherrealm },
        new() { Id = "YJ_LiuJinJieQu200", Name = "新月長廊", Type = MapType.Netherrealm },
        new() { Id = "SD_ShengHuoLing0203", Name = "霧雨密林", Type = MapType.Netherrealm },
        new() { Id = "SQ_XiongShiZhiXin200", Name = "王者樞紐", Type = MapType.Netherrealm },
        new() { Id = "SQ_NvShenQunBai100", Name = "不潔綠洲", Type = MapType.Netherrealm },
        new() { Id = "JH_JiaoTangDaTing000", Name = "禱告聖堂", Type = MapType.Netherrealm },
        new() { Id = "SD_DuiLongJuQiang211", Name = "雲間高牆", Type = MapType.Netherrealm },
        new() { Id = "YJ_RiXiShenMiao200", Name = "日棲神廟", Type = MapType.Netherrealm },
        new() { Id = "DD_JueWangZhiQiang000", Name = "無垢之牆", Type = MapType.Netherrealm },
        new() { Id = "JH_TongKuMiDian000", Name = "苦罰秘殿", Type = MapType.Netherrealm },
        new() { Id = "SD_YuanGuTongDao101", Name = "聚獸平原", Type = MapType.Netherrealm },
        new() { Id = "DD_YinYanJieXiang200", Name = "遺落街巷", Type = MapType.Netherrealm },
        new() { Id = "DD_ZaWuJieQu000", Name = "雜蕪街區", Type = MapType.Netherrealm }
    ];

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
        _mapConfigs = [.. DefaultMapConfigs];
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
