using System.Text.Json.Serialization;
using TorchLight.Statistics.Enums;

namespace TorchLight.Statistics.Models
{
    public class ItemBaseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ItemType Type { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PageIdType PageIdType { get; set; }
        public bool Enable { get; set; } = true;
        public bool Watch { get; set; }
        public int Like { get; set; } = 0;
    }
}
