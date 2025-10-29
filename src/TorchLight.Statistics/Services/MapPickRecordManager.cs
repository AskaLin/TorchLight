using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.Services;

/// <summary>
/// 管理異界地圖的拾取記錄
/// </summary>
public class MapPickRecordManager
{
    private readonly List<MapRecordModel> _mapRecords = [];
    private MapRecordModel _currentMapRecord = new();
    private Dictionary<int, PickedItemDataModel> _currentMapPickData = [];
    private readonly Dictionary<int, string> _itemIdTable;

    public MapPickRecordManager(Dictionary<int, string> itemIdTable)
    {
        _itemIdTable = itemIdTable;
    }

    public bool IsInNetherrealmMap { get; private set; }
    public string CurrentMapName { get; private set; } = string.Empty;
    public IReadOnlyList<MapRecordModel> MapRecords => _mapRecords;

    /// <summary>
    /// 開始記錄新地圖
    /// </summary>
    public void StartMapRecord(string mapId, string mapName, DateTime startTime)
    {
        _currentMapRecord = new MapRecordModel
        {
            Id = mapId,
            Name = mapName,
            StartTime = startTime
        };
        _currentMapPickData = [];
        IsInNetherrealmMap = true;
        CurrentMapName = mapName;

        Console.WriteLine($"{startTime:yyyy/MM/dd HH:mm:ss}\t進入異界地圖 {mapName} 開始統計拾取物品");
    }

    /// <summary>
    /// 結束當前地圖記錄
    /// </summary>
    public void EndMapRecord(string mapName, DateTime endTime)
    {
        _currentMapRecord.EndTime = endTime;
        _currentMapRecord.PickRecord = _currentMapPickData;
        _mapRecords.Add(_currentMapRecord);

        Console.WriteLine($"{endTime:yyyy/MM/dd HH:mm:ss}\t離開異界地圖 {mapName} 紀錄拾取物品");

        // 重置
        _currentMapRecord = new();
        _currentMapPickData = [];
        IsInNetherrealmMap = false;

        // 顯示所有記錄
        PrintAllRecords();
    }

    /// <summary>
    /// 更新當前地圖名稱
    /// </summary>
    public void UpdateCurrentMapName(string mapName)
    {
        CurrentMapName = mapName;
    }

    /// <summary>
    /// 記錄拾取物品
    /// </summary>
    public MapPickResult RecordPickedItem(int configBaseId, int slotId, int quantityChange)
    {
        if (!IsInNetherrealmMap)
        {
            return null;
        }

        var result = new MapPickResult
        {
            ItemName = GetItemName(configBaseId),
            ConfigBaseId = configBaseId,
            SlotId = slotId,
            QuantityChange = quantityChange
        };

        if (_currentMapPickData.TryGetValue(configBaseId, out var existingItem))
        {
            if (existingItem.Slots.TryGetValue(slotId, out int previousSlotCount))
            {
                // 更新現有欄位
                existingItem.Slots[slotId] = previousSlotCount + quantityChange;
                existingItem.Total += quantityChange;

                result.PreviousSlotCount = previousSlotCount;
                result.NewSlotCount = existingItem.Slots[slotId];
                result.NewTotalCount = existingItem.Total;
                result.IsExistingSlot = true;
            }
            else
            {
                // 新欄位
                existingItem.Slots[slotId] = quantityChange;
                existingItem.Total += quantityChange;

                result.NewSlotCount = quantityChange;
                result.NewTotalCount = existingItem.Total;
                result.IsNewSlot = true;
            }
        }
        else
        {
            // 新物品
            var newItem = new PickedItemDataModel
            {
                BaseId = configBaseId,
                Name = result.ItemName,
                Total = quantityChange
            };
            newItem.Slots[slotId] = quantityChange;
            _currentMapPickData[configBaseId] = newItem;

            result.NewSlotCount = quantityChange;
            result.NewTotalCount = quantityChange;
            result.IsFirstTimeInMap = true;
        }

        return result;
    }

    /// <summary>
    /// 重置所有記錄（登入時使用）
    /// </summary>
    public void Reset()
    {
        _mapRecords.Clear();
        _currentMapRecord = new();
        _currentMapPickData = [];
        IsInNetherrealmMap = false;
        CurrentMapName = string.Empty;
    }

    /// <summary>
    /// 顯示所有記錄
    /// </summary>
    public void PrintAllRecords()
    {
        foreach (var record in _mapRecords)
        {
            Console.WriteLine($"地圖: {record.Name} ({record.UseTime}), ID: {record.Id}");
            foreach (var item in record.PickRecord)
            {
                Console.WriteLine($"\t{item.Value.Name}: {item.Value.Total}");
            }
        }
    }

    private string GetItemName(int configBaseId)
    {
        return _itemIdTable.TryGetValue(configBaseId, out string itemName) ? itemName : $"未知的物品({configBaseId})";
    }
}

/// <summary>
/// 地圖拾取結果
/// </summary>
public class MapPickResult
{
    public string ItemName { get; set; }
    public int ConfigBaseId { get; set; }
    public int SlotId { get; set; }
    public int QuantityChange { get; set; }
    public int PreviousSlotCount { get; set; }
    public int NewSlotCount { get; set; }
    public int NewTotalCount { get; set; }
    public bool IsFirstTimeInMap { get; set; }
    public bool IsNewSlot { get; set; }
    public bool IsExistingSlot { get; set; }
}
