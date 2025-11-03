using System.Text.Json.Serialization;
using TorchLight.Statistics.Enums;

namespace TorchLight.Statistics.Models;

/// <summary>
/// 地圖設定項目（已棄用，請使用 MapIdConfig）
/// </summary>
[Obsolete("此類別已棄用，請使用 MapIdConfig 代替（使用 int Id 而非 string Id）")]
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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MapType Type { get; set; }   
}
