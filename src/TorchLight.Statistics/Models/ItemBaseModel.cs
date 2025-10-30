using TorchLight.Statistics.Enums;

namespace TorchLight.Statistics.Models
{
    public class ItemBaseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public PageIdType PageIdType { get; set; }
        public bool Enable { get; set; } = true;
    }
}
