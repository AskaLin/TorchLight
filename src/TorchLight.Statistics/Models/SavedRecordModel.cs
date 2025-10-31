using System.Text.Json.Serialization;

namespace TorchLight.Statistics.Models;

/// <summary>
/// 保存的記錄模型
/// </summary>
public class SavedRecordModel
{
    /// <summary>
    /// 統計摘要
    /// </summary>
    [JsonPropertyName("summary")]
    public RecordSummary Summary { get; set; }

    /// <summary>
    /// 地圖記錄列表
    /// </summary>
    [JsonPropertyName("records")]
    public List<MapRecordModel> Records { get; set; }

    /// <summary>
    /// 保存時間
    /// </summary>
    [JsonPropertyName("savedTime")]
    public DateTime SavedTime { get; set; }
}

/// <summary>
/// 記錄摘要
/// </summary>
public class RecordSummary
{
    /// <summary>
    /// 總地圖數
    /// </summary>
    [JsonPropertyName("totalMaps")]
    public int TotalMaps { get; set; }

    /// <summary>
    /// 總物品種類
    /// </summary>
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    /// <summary>
    /// 總數量
    /// </summary>
    [JsonPropertyName("totalQuantity")]
    public int TotalQuantity { get; set; }

    /// <summary>
    /// 總遊戲時間
    /// </summary>
    [JsonPropertyName("totalPlayTime")]
    public string TotalPlayTime { get; set; }

    /// <summary>
    /// 最常拾取的物品（前10）
    /// </summary>
    [JsonPropertyName("mostPickedItems")]
    public List<TopPickedItem> MostPickedItems { get; set; }
}

/// <summary>
/// 最常拾取的物品
/// </summary>
public class TopPickedItem
{
    /// <summary>
    /// 物品 BaseId
    /// </summary>
    [JsonPropertyName("baseId")]
    public int BaseId { get; set; }

    /// <summary>
    /// 物品名稱
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// 總數量
    /// </summary>
    [JsonPropertyName("totalQuantity")]
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Like 值
    /// </summary>
  [JsonPropertyName("like")]
    public int Like { get; set; }
}
