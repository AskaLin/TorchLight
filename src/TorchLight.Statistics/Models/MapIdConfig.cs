using System.Text.Json.Serialization;
using TorchLight.Statistics.Enums;

namespace TorchLight.Statistics.Models;

/// <summary>
/// 地圖設定項目
/// </summary>
public class MapIdConfig
{
    /// <summary>
    /// 地圖ID
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    /// <summary>
    /// 地圖名稱
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 地圖類型（Hideout、Netherrealm、SecretRealm）
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MapType Type { get; set; }
}
