using System.Text.Json.Serialization;

namespace TorchLight.Statistics.Models;

/// <summary>
/// 地圖設定項目
/// </summary>
public class MapConfigItem
{
    /// <summary>
    /// 地圖ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 地圖名稱
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 地圖類型（Hideout、Netherrealm、SecretRealm）
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    // 向後兼容的屬性
    [JsonIgnore]
    public string MapId
    {
        get => Id;
        set => Id = value;
    }

    [JsonIgnore]
    public string MapName
    {
        get => Name;
        set => Name = value;
    }

    [JsonIgnore]
    public string MapType
    {
        get => Type;
        set => Type = value;
    }
}
