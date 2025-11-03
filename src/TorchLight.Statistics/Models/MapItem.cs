using System.Text.Json.Serialization;
using TorchLight.Statistics.Enums;

namespace TorchLight.Statistics.Models
{
    /// <summary>
    /// JSON檔案對應類別
    /// </summary>
    public class MapItem
    {
        [JsonPropertyName("mapIds")]
        public List<int> MapIds { get; set; } = [];

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MapType Type { get; set; }
    }
}
