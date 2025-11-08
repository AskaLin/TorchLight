namespace TorchLight.Statistics.Models;

public class PickedItemDataModel
{
    public int BaseId { get; set; }
    public string Name { get; set; }    
    public Dictionary<int, int> Slots { get; set; } = [];    
    public int Total { get; set; }
    
    /// <summary>
    /// ✅ 新增：物品的 Like 值 (0-6)
    /// </summary>
    public int Like { get; set; }
    
    /// <summary>
    /// ✅ 新增：物品類型（用於判斷是否為未知物品）
    /// </summary>
    public string ItemType { get; set; }
    
    /// <summary>
  /// ✅ 新增：PageId 類型
    /// </summary>
    public int PageId { get; set; }
}

