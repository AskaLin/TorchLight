using TorchLight.Statistics.Core;

namespace TorchLight.Statistics;

/// <summary>
/// 地圖映射器 - 負責地圖ID與名稱的轉換，以及地圖類型的判斷
/// </summary>
public class MapMapper
{
    /// <summary>
    /// 地圖ID到名稱的映射表
    /// </summary>
    private static readonly Dictionary<string, string> _mapNameMapping = new()
    {
        { "XZ_YuJinZhiXiBiNanSuo200", "餘燼之息避難所" },
        { "GeBuLinCunLuo01", "隔壁林村落01" },
        { "YJ_TaiYangWangTing200", "長明宮城" }
    };

    /// <summary>
    /// 藏身處地圖ID集合
    /// </summary>
    private static readonly HashSet<string> _hideoutMapIds =
    [
        "XZ_YuJinZhiXiBiNanSuo200"
    ];

    /// <summary>
    /// 異界地圖ID集合（可統計拾取的地圖）
    /// </summary>
    private static readonly HashSet<string> _netherrealmMapIds =
    [
        "GeBuLinCunLuo01",
        "YJ_TaiYangWangTing200"
    ];

    /// <summary>
    /// 從完整路徑獲取地圖資訊
    /// </summary>
    public static MapInfo GetMapInfo(string fullMapPath)
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
        return _mapNameMapping.TryGetValue(mapId, out var name) ? name : mapId;
    }

    /// <summary>
    /// 判斷是否為藏身處地圖
    /// </summary>
    public static bool IsHideoutMap(string mapIdOrPath)
    {
        var mapId = mapIdOrPath.Contains('/') ? ExtractMapId(mapIdOrPath) : mapIdOrPath;
        return _hideoutMapIds.Contains(mapId);
    }

    /// <summary>
    /// 判斷是否為異界地圖
    /// </summary>
    public static bool IsNetherrealmMap(string mapIdOrPath)
    {
        var mapId = mapIdOrPath.Contains('/') ? ExtractMapId(mapIdOrPath) : mapIdOrPath;
        return _netherrealmMapIds.Contains(mapId);
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
    /// 新增地圖映射（用於動態擴展）
    /// </summary>
    public static void AddMapMapping(string mapId, string mapName, MapType mapType)
    {
        _mapNameMapping[mapId] = mapName;

        switch (mapType)
        {
            case MapType.Hideout:
                _hideoutMapIds.Add(mapId);
                break;
            case MapType.Netherrealm:
                _netherrealmMapIds.Add(mapId);
                break;
        }
    }
}
