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
    private readonly Dictionary<int, ItemModel> _itemTable;

    // 暫存開圖材料
    private string _pendingMapTicket = string.Empty;
    private readonly List<string> _pendingCompasses = [];
    private string _pendingProbe = string.Empty;

    public MapPickRecordManager(Dictionary<int, ItemModel> itemTable)
    {
        _itemTable = itemTable;
    }

    public bool IsInNetherrealmMap { get; private set; }
    public string CurrentMapName { get; private set; } = string.Empty;
    public IReadOnlyList<MapRecordModel> MapRecords => _mapRecords;

    /// <summary>
    /// 記錄開圖材料（從 Spv3Open 事件）
    /// </summary>
    public void RecordMapMaterial(int configBaseId, ItemType itemType)
    {
        if (!_itemTable.TryGetValue(configBaseId, out var item))
            return;

        switch (itemType)
        {
            case ItemType.MapTicket:
            case ItemType.BossTicket:
            case ItemType.GameplayTicket:
                _pendingMapTicket = item.Name;
                Console.WriteLine($"[開圖材料] 門票: {item.Name}");
                break;

            case ItemType.Compass:
                if (_pendingCompasses.Count < 4)
                {
                    _pendingCompasses.Add(item.Name);
                    Console.WriteLine($"[開圖材料] 羅盤 #{_pendingCompasses.Count}: {item.Name}");
                }
                break;

            case ItemType.Probe:
                _pendingProbe = item.Name;
                Console.WriteLine($"[開圖材料] 探針: {item.Name}");
                break;
        }
    }

    /// <summary>
    /// 開始記錄新地圖
    /// </summary>
    public void StartMapRecord(string mapId, string mapName, DateTime startTime)
    {
        _currentMapRecord = new MapRecordModel
        {
            Id = mapId,
            Name = mapName,
            StartTime = startTime,
            MapTicket = _pendingMapTicket,
            Probe = _pendingProbe
        };

        // 複製羅盤資料到陣列
        for (int i = 0; i < _pendingCompasses.Count && i < 4; i++)
        {
            _currentMapRecord.Compass[i] = _pendingCompasses[i];
        }

        _currentMapPickData = [];
        IsInNetherrealmMap = true;
        CurrentMapName = mapName;

        Console.WriteLine($"{startTime:yyyy/MM/dd HH:mm:ss}\t進入異界地圖 {mapName}");
        if (!string.IsNullOrEmpty(_pendingMapTicket))
        {
            Console.WriteLine($"\t使用門票: {_pendingMapTicket}");
        }
        if (_pendingCompasses.Count > 0)
        {
            Console.WriteLine($"\t使用羅盤: {string.Join(", ", _pendingCompasses)}");
        }
        if (!string.IsNullOrEmpty(_pendingProbe))
        {
            Console.WriteLine($"\t使用探針: {_pendingProbe}");
        }
        Console.WriteLine("\t開始統計拾取物品");

        // 清空暫存資料
        ClearPendingMaterials();
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

        ClearPendingMaterials();
    }

    /// <summary>
    /// 清空暫存的開圖材料
    /// </summary>
    private void ClearPendingMaterials()
    {
        _pendingMapTicket = string.Empty;
        _pendingCompasses.Clear();
        _pendingProbe = string.Empty;
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
        return _itemTable.TryGetValue(configBaseId, out var item) ? item.Name : $"未知的物品({configBaseId})";
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
