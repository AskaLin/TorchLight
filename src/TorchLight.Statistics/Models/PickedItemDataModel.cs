namespace TorchLight.Statistics.Models;

public class PickedItemDataModel
{
    public int BaseId { get; set; }
    public string Name { get; set; }    
    public Dictionary<int, int> Slots { get; set; } = [];    
    public int Total { get; set; }
}

