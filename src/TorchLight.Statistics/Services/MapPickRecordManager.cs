using Serilog;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Mapper;
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
    private int _pendingResonance = 0;
    

    public MapPickRecordManager(Dictionary<int, ItemModel> itemTable)
    {
        _itemTable = itemTable;
    }

    public bool IsInNetherrealmMap { get; private set; }
    public string CurrentMapName { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public IReadOnlyList<MapRecordModel> MapRecords => _mapRecords;

    public void SetMapToken(string token)
    {
        Token = token;
    }
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
                Log.Debug("[開圖材料] 門票: {TicketName}", item.Name);
                break;

            case ItemType.Compass:
                if (_pendingCompasses.Count < 4)
                {
                    _pendingCompasses.Add(item.Name);
                    Log.Debug("[開圖材料] 羅盤 #{Index}: {CompassName}", _pendingCompasses.Count, item.Name);
                }
                break;

            case ItemType.Probe:
                _pendingProbe = item.Name;
                Log.Debug("[開圖材料] 探針: {ProbeName}", item.Name);
                break;
            case ItemType.Currency:
                _pendingResonance = item.Num;
                Log.Debug("[開圖材料] 迴響: {count}", item.Num);
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
            Id = MapInfoMapper.ExtractMapId(mapId),
            Name = mapName,
            StartTime = startTime,
            MapTicket = _pendingMapTicket,
            Probe = _pendingProbe,
            Token = Token            
        };

        // 複製羅盤資料到陣列
        for (int i = 0; i < _pendingCompasses.Count && i < 4; i++)
        {
            _currentMapRecord.Compass[i] = _pendingCompasses[i];
        }

        _currentMapPickData = [];
        IsInNetherrealmMap = true;
        CurrentMapName = mapName;

        Log.Information("{Time} 進入異界地圖: {MapName} {tk}", startTime.ToString("yyyy/MM/dd HH:mm:ss"), CurrentMapName, _currentMapRecord.Token);

        if (!string.IsNullOrEmpty(_pendingMapTicket))
        {
            Log.Information("  使用門票: {Ticket}", _pendingMapTicket);
        }
        if (_pendingCompasses.Count > 0)
        {
            Log.Information("  使用羅盤: {Compasses}", string.Join(", ", _pendingCompasses));
        }
        if (!string.IsNullOrEmpty(_pendingProbe))
        {
            Log.Information("  使用探針: {Probe}", _pendingProbe);
        }

        // 清空暫存資料
        ClearPendingMaterials();
    }

    /// <summary>
    /// 結束當前地圖記錄
    /// </summary>
    public void EndMapRecord(DateTime endTime)
    {
        _currentMapRecord.EndTime = endTime;
        _currentMapRecord.PickRecord = _currentMapPickData;
        _mapRecords.Add(_currentMapRecord);

        Log.Information("{Time} 離開異界地圖: {MapName} (用時: {Duration})", endTime.ToString("yyyy/MM/dd HH:mm:ss"), CurrentMapName, _currentMapRecord.UseTime);

        // 顯示當前地圖的拾取記錄
        PrintCurrentMapRecord(_currentMapRecord);

        // 重置
        _currentMapRecord = new();
        _currentMapPickData = [];
        IsInNetherrealmMap = false;
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

        // 檢查物品是否啟用統計
        if (!ItemInfoMapper.IsItemEnabled(configBaseId))
        {
            Log.Debug("[拾取統計] 物品 {ItemId} 已停用，跳過記錄", configBaseId);
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
    /// 獲取當前地圖記錄（包含完整資訊）
    /// </summary>
    public MapRecordModel? GetCurrentMapRecord()
    {
        if (!IsInNetherrealmMap || _currentMapRecord == null)
            return null;

        // 創建一個包含當前拾取記錄的副本
        var recordCopy = new MapRecordModel
        {
            RecordId = _currentMapRecord.RecordId,
            Id = _currentMapRecord.Id,
            Name = _currentMapRecord.Name,
            MapTicket = _currentMapRecord.MapTicket,
            Probe = _currentMapRecord.Probe,
            StartTime = _currentMapRecord.StartTime,
            EndTime = DateTime.Now, // 當前時間作為臨時結束時間
            PickRecord = _currentMapPickData
        };

        // 複製羅盤資料
        for (int i = 0; i < 4; i++)
        {
            recordCopy.Compass[i] = _currentMapRecord.Compass[i];
        }

        return recordCopy;
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
        Token = string.Empty;
        _pendingResonance = 0;
    }

    /// <summary>
    /// 顯示當前地圖的拾取記錄
    /// </summary>
    private void PrintCurrentMapRecord(MapRecordModel record)
    {
        if (record.PickRecord == null || record.PickRecord.Count == 0)
        {
            Log.Information("本次地圖未拾取任何物品");
            return;
        }

        Log.Information("═══ 本次地圖拾取統計 ═══");
        Log.Information("地圖: {MapName} (用時: {Duration})", record.Name, record.UseTime);

        foreach (var item in record.PickRecord.OrderByDescending(x => x.Value.Total))
        {
            Log.Information("  {ItemName}: {Total} 個", item.Value.Name, item.Value.Total);
        }

        Log.Information("═══════════════════════");
    }

    /// <summary>
    /// 顯示所有記錄（用於 Debug）
    /// </summary>
    public void PrintAllRecords()
    {
        Log.Debug("所有地圖記錄統計 (共 {Count} 筆):", _mapRecords.Count);
        foreach (var record in _mapRecords)
        {
            Log.Debug("  [{RecordId}] {MapName} (用時: {Duration})", record.RecordId, record.Name, record.UseTime);
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
