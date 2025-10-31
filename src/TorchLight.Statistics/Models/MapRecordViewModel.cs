using TorchLight.Statistics.Enums;

namespace TorchLight.Statistics.Models;

/// <summary>
/// 地圖記錄視圖模型 - 用於前端顯示
/// </summary>
public class MapRecordViewModel
{
    public MapRecordViewModel()
    {

    }

    public MapRecordViewModel(bool isInMap, MapType mapType, string mapName)
    {
        IsInMap = isInMap;
        MapType = mapType.ToString();
        MapName = mapName;
    }    

    /// <summary>
    /// 是否在地圖中
    /// </summary>
    public bool IsInMap { get; set; } = false;

    /// <summary>
    /// 地圖類型
    /// </summary>
    public string MapType { get; set; } = string.Empty;

    /// <summary>
    /// 地圖名稱
    /// </summary>
    public string MapName { get; set; } = string.Empty;    

    /// <summary>
    /// 記錄唯一識別碼
    /// </summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// 使用門票
    /// </summary>
    public string MapTicket { get; set; } = string.Empty;

    /// <summary>
    /// 使用羅盤列表
    /// </summary>
    public string[] Compass { get; set; } = [];

    /// <summary>
    /// 使用探針
    /// </summary>
    public string Probe { get; set; } = string.Empty;

    /// <summary>
    /// 開始時間
    /// </summary>
    public DateTime? StartTime { get; set; } = null;

    /// <summary>
    /// 拾取物品列表
    /// </summary>
    public PickedItemViewModel[] Items { get; set; } = [];
}

/// <summary>
/// 拾取物品視圖模型
/// </summary>
public class PickedItemViewModel
{
    /// <summary>
    /// 物品 BaseId
    /// </summary>
    public int BaseId { get; set; }

    /// <summary>
    /// 物品名稱
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 總數量
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 欄位數量分布
    /// </summary>
    public Dictionary<int, int> Slots { get; set; }
}
