using System.Text.Json.Serialization;

namespace TorchLight.Statistics.Models;

/// <summary>
/// 地圖設定檔資料模型
/// </summary>
public class MapInfoConfig
{
    /// <summary>
    /// 地圖名稱映射
    /// </summary>
  [JsonPropertyName("mapNameMapping")]
    public Dictionary<string, string> MapNameMapping { get; set; } = new();

    /// <summary>
    /// 藏身處地圖ID列表
    /// </summary>
    [JsonPropertyName("hideoutMapIds")]
    public List<string> HideoutMapIds { get; set; } = new();

    /// <summary>
    /// 異界地圖ID列表
    /// </summary>
[JsonPropertyName("netherrealmMapIds")]
    public List<string> NetherrealmMapIds { get; set; } = new();
}

/// <summary>
/// 地圖設定項目（前端使用）
/// </summary>
public class MapConfigItem
{
    /// <summary>
    /// 地圖ID
    /// </summary>
    [JsonPropertyName("mapId")]
    public string MapId { get; set; } = string.Empty;

    /// <summary>
    /// 地圖名稱
    /// </summary>
    [JsonPropertyName("mapName")]
    public string MapName { get; set; } = string.Empty;

    /// <summary>
    /// 地圖類型（Hideout 或 Netherrealm）
  /// </summary>
    [JsonPropertyName("mapType")]
    public string MapType { get; set; } = string.Empty;
}
