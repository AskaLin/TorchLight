using Serilog;
using TorchLight.Statistics.LogProcessor;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.Services;

/// <summary>
/// 管理玩家背包庫存
/// </summary>
public class BagInventoryManager
{
    private readonly Dictionary<int, PickedItemDataModel> _bagData = [];

    public IReadOnlyDictionary<int, PickedItemDataModel> BagData => _bagData;

    /// <summary>
    /// 初始化背包物品
    /// </summary>
    public void InitializeBagItem(ItemModel item)
    {
        if (_bagData.TryGetValue(item.ConfigBaseId, out var existingItem))
        {
            existingItem.Total += item.Num;
            existingItem.Slots[item.SoltId] = item.Num;
            return;
        }

        var bagData = new PickedItemDataModel
        {
            BaseId = item.ConfigBaseId,
            Name = ItemInfoMapper.GetItemName(item.ConfigBaseId),
            Total = item.Num
        };

        bagData.Slots[item.SoltId] = item.Num;
        _bagData[item.ConfigBaseId] = bagData;
    }

    /// <summary>
    /// 更新背包物品數量
    /// </summary>
    public ItemChangeResult UpdateBagItem(ItemChangeEvent ev)
    {
        var result = new ItemChangeResult
        {
            ItemName = ItemInfoMapper.GetItemName(ev.ConfigBaseId),
            ConfigBaseId = ev.ConfigBaseId,
            SlotId = ev.SlotId,
            NewSlotCount = ev.Num
        };

        if (!_bagData.TryGetValue(ev.ConfigBaseId, out var bagItem))
        {
            // 新物品
            var newItem = new PickedItemDataModel
            {
                BaseId = ev.ConfigBaseId,
                Name = result.ItemName,
                Total = ev.Num
            };
            newItem.Slots[ev.SlotId] = ev.Num;
            _bagData[ev.ConfigBaseId] = newItem;

            result.IsNewItem = true;
            result.QuantityChange = ev.Num;
            result.NewTotalCount = ev.Num;
            return result;
        }

        result.PreviousTotalCount = bagItem.Total;

        if (bagItem.Slots.TryGetValue(ev.SlotId, out int previousSlotCount))
        {
            // 更新現有欄位
            int quantityChange = ev.Num - previousSlotCount;
            bagItem.Slots[ev.SlotId] = ev.Num;
            bagItem.Total += quantityChange;

            result.PreviousSlotCount = previousSlotCount;
            result.QuantityChange = quantityChange;
            result.NewTotalCount = bagItem.Total;
        }
        else
        {
            // 新欄位
            bagItem.Slots[ev.SlotId] = ev.Num;
            bagItem.Total += ev.Num;

            result.IsNewSlot = true;
            result.QuantityChange = ev.Num;
            result.NewTotalCount = bagItem.Total;
        }

        return result;
    }

    /// <summary>
    /// 重置背包資料（登入時使用）
    /// </summary>
    public void Reset()
    {
        _bagData.Clear();
    }

    /// <summary>
    /// 顯示背包初始化完成的資訊
    /// </summary>
    public void PrintInitializedBag()
    {
        Log.Debug("背包初始化明細:");
        foreach (var bagItem in _bagData)
        {
            Log.Debug("  {ItemName}({ItemId}): {Total} 個", bagItem.Value.Name, bagItem.Value.BaseId, bagItem.Value.Total);
        }
    }
}

/// <summary>
/// 物品變更結果
/// </summary>
public class ItemChangeResult
{
    public string ItemName { get; set; }
    public int ConfigBaseId { get; set; }
    public int SlotId { get; set; }
    public int NewSlotCount { get; set; }
    public int PreviousSlotCount { get; set; }
    public int PreviousTotalCount { get; set; }
    public int NewTotalCount { get; set; }
    public int QuantityChange { get; set; }
    public bool IsNewItem { get; set; }
    public bool IsNewSlot { get; set; }
}
