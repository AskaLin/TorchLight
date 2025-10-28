namespace TorchLight.Statistics.Models;

public class BagDataModel
{
    public string ItemName { get; set; }
    public Dictionary<int, int> Slots { get; set; } = [];
    public int Total { get; set; }
}

