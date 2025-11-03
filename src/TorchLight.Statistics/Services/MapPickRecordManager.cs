using Serilog;
using System.Text.Json;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.Services;

/// <summary>
/// 管理異界地圖的拾取記錄
/// </summary>
public class MapPickRecordManager(Dictionary<int, ItemModel> itemTable)
{
    private readonly List<MapRecordModel> _mapRecords = [];
    private MapRecordModel _currentMapRecord = new();
    private Dictionary<int, PickedItemDataModel> _currentMapPickData = [];
    private readonly Dictionary<int, ItemModel> _itemTable = itemTable;
    private static readonly JsonSerializerOptions _ops = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };


    // 🆕 存檔目錄
    private static readonly string SavedDirectory = Path.Combine(AppContext.BaseDirectory, "Saved");

    public bool IsInMap { get; private set; }
    public string CurrentMapName { get; private set; } = string.Empty;

    public IReadOnlyList<MapRecordModel> MapRecords => _mapRecords;

    public void SetMapToken(string token)
    {
        _currentMapRecord.RecordId = token;
        Log.Debug("設定 Map Token {tok}", _currentMapRecord.RecordId);
    }
    public void SetMapId(int mapId)
    {
        _currentMapRecord.MapId = mapId;
        var mapIdConfig = MapInfoMapper.GetMapInfo(mapId);
        if (mapIdConfig != null)
        {
            _currentMapRecord.Name = mapIdConfig.GetDisplayName();
            _currentMapRecord.Type = mapIdConfig.Type;
            Log.Debug("設定 Map ID {id} Name {name} ", _currentMapRecord.MapId, _currentMapRecord.Name);
        }
    }
    public void SetMapLevel(int mapLevel)
    {
        _currentMapRecord.Level = mapLevel;
        Log.Debug("設定 Map Level {level}", _currentMapRecord.Level);
    }
    public bool CurrentMapRecordInfoComplete()
    {
        return _currentMapRecord.MapInfoComplete();
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
                _currentMapRecord.MapTicket = item.Name;
                // _currentMapRecord.MapTicketId = item.ConfigBaseId;
                Log.Debug("[開圖材料] 門票: {TicketName}", item.Name);
                break;

            case ItemType.Compass:
                _currentMapRecord.Compass.Add(item.Name);
                Log.Debug("[開圖材料] 羅盤 #{Index}: {CompassName}", _currentMapRecord.Compass.Count, item.Name);
                break;

            case ItemType.Probe:
                _currentMapRecord.Probe = item.Name;
                Log.Debug("[開圖材料] 探針: {ProbeName}", item.Name);
                break;

            case ItemType.Currency:
                _currentMapRecord.Resonance = item.Num;
                Log.Debug("[開圖材料] 迴響: {count}", item.Num);
                break;
        }
    }

    /// <summary>
    /// 開始記錄新地圖
    /// </summary>
    public void StartMapRecord(DateTime startTime)
    {
        _currentMapRecord.StartTime = startTime;

        _currentMapPickData = [];
        IsInMap = true;
        CurrentMapName = _currentMapRecord.Name; // 先不動CurrentMapName，之後在處理他

        Log.Information("{Time} 進入異界地圖: {MapName}({Token})", startTime.ToString("yyyy/MM/dd HH:mm:ss"), _currentMapRecord.Name, _currentMapRecord.RecordId);

        if (!string.IsNullOrEmpty(_currentMapRecord.MapTicket))
        {
            Log.Information("  使用門票: {Ticket}", _currentMapRecord.MapTicket);
        }
        if (_currentMapRecord.Compass.Count != 0)
        {
            Log.Information("  使用羅盤: {Compasses}", string.Join(", ", _currentMapRecord.Compass));
        }
        if (!string.IsNullOrEmpty(_currentMapRecord.Probe))
        {
            Log.Information("  使用探針: {Probe}", _currentMapRecord.Probe);
        }
        if (_currentMapRecord.Resonance > 0)
        {
            Log.Information("  使用迴響: {Resonance}", _currentMapRecord.Resonance);
        }
    }

    /// <summary>
    /// 結束當前地圖記錄
    /// </summary>
    public void EndMapRecord(DateTime endTime)
    {
        if (_currentMapRecord.StartTime == DateTime.MinValue)
        {
            Log.Warning("嘗試結束地圖記錄，但當前沒有有效的地圖記錄");
            Reset();
            Log.Warning("重置地圖記錄狀態");
            return;
        }

        _currentMapRecord.EndTime = endTime;
        _currentMapRecord.PickRecord = _currentMapPickData;
        _mapRecords.Add(_currentMapRecord);

        Log.Information("{Time} 離開異界地圖: {MapName}({Token}) - 用時: {Duration}", endTime.ToString("yyyy/MM/dd HH:mm:ss"), _currentMapRecord.Name, _currentMapRecord.RecordId, _currentMapRecord.UseTime);

        // 顯示當前地圖的拾取記錄
        PrintCurrentMapRecord(_currentMapRecord);

        // 🆕 自動存檔
        SaveRecordsToFile();

        // 重置
        _currentMapRecord = new();
        _currentMapPickData = [];
        IsInMap = false;
    }

    /// <summary>
    /// 記錄拾取物品
    /// </summary>
    public MapPickResult RecordPickedItem(int configBaseId, int slotId, int quantityChange)
    {
        if (!IsInMap)
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
            ItemName = _itemTable.TryGetValue(configBaseId, out var item) ? item.Name : $"未知的物品({configBaseId})",
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
    public MapRecordModel GetCurrentMapRecord()
    {
        if (!IsInMap || _currentMapRecord == null)
            return null;

        // 創建一個包含當前拾取記錄的副本
        var recordCopy = new MapRecordModel
        {
            RecordId = _currentMapRecord.RecordId,
            // Id = _currentMapRecord.Id,
            Name = _currentMapRecord.Name,
            MapTicket = _currentMapRecord.MapTicket,
            // MapTicketId = _currentMapRecord.MapTicketId,
            Compass = _currentMapRecord.Compass,
            Probe = _currentMapRecord.Probe,
            StartTime = _currentMapRecord.StartTime,
            EndTime = DateTime.Now, // 當前時間作為臨時結束時間
            PickRecord = _currentMapPickData
        };

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
        IsInMap = false;
        CurrentMapName = string.Empty;
    }

    /// <summary>
    /// 顯示當前地圖的拾取記錄
    /// </summary>
    private static void PrintCurrentMapRecord(MapRecordModel record)
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

    // 🆕 自動存檔功能
    private void SaveRecordsToFile()
    {
        try
        {
            if (_mapRecords.Count == 0)
                return;

            // 確保目錄存在
            if (!Directory.Exists(SavedDirectory))
            {
                Directory.CreateDirectory(SavedDirectory);
            }

            // 生成檔案名稱：TorchPickRecord_MMdd_HHmm.json
            var firstRecord = _mapRecords[0];
            var fileName = $"TorchPickRecord_{firstRecord.StartTime:MMdd_HHmm}.json";
            var filePath = Path.Combine(SavedDirectory, fileName);

            // 準備保存的資料
            var savedRecord = new SavedRecordModel
            {
                Summary = GenerateSummary(),
                Records = [.. _mapRecords],
                SavedTime = DateTime.Now
            };

            // 序列化並寫入檔案           
            var json = JsonSerializer.Serialize(savedRecord, _ops);
            File.WriteAllText(filePath, json);

            Log.Information("記錄已自動保存至: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "自動保存記錄失敗");
        }
    }

    // 🆕 生成統計摘要
    private RecordSummary GenerateSummary()
    {
        var totalPlayTime = TimeSpan.FromSeconds(_mapRecords.Sum(r => (r.EndTime - r.StartTime).TotalSeconds));

        // 收集所有拾取的物品
        var allItems = _mapRecords
            .SelectMany(r => r.PickRecord?.Values ?? Enumerable.Empty<PickedItemDataModel>())
            .GroupBy(p => p.BaseId)
            .Select(g => new
            {
                BaseId = g.Key,
                g.First().Name,
                TotalQuantity = g.Sum(p => p.Total),
                // 🔧 修正：從 ItemInfoMapper 獲取 Like 值
                Like = ItemInfoMapper.GetAllItemConfigs().FirstOrDefault(item => item.Id == g.Key)?.Like ?? 0
            })
            .Where(x => x.TotalQuantity > 0) // 排除數量為 0 的物品
            .OrderByDescending(x => x.Like) // 先按 Like 排序
            .ThenByDescending(x => x.TotalQuantity) // Like 相同時按數量排序
            .Take(10).ToList();

        return new RecordSummary
        {
            TotalMaps = _mapRecords.Count,
            TotalItems = _mapRecords.Sum(r => r.PickRecord?.Count ?? 0),
            TotalQuantity = _mapRecords.Sum(r => r.PickRecord?.Select(p => p.Value.Total).Sum() ?? 0),
            TotalPlayTime = totalPlayTime.ToString(@"hh\:mm\:ss"),
            MostPickedItems = [.. allItems.Select(x => new TopPickedItem
            {
                BaseId = x.BaseId,
                Name = x.Name,
                TotalQuantity = x.TotalQuantity,
                Like = x.Like
            })]
        };
    }

    // 🆕 獲取所有歷史記錄檔案
    public static List<string> GetSavedRecordFiles()
    {
        try
        {
            if (!Directory.Exists(SavedDirectory))
                return [];

            return [.. Directory.GetFiles(SavedDirectory, "TorchPickRecord_*.json").OrderByDescending(f => f)];
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取歷史記錄檔案失敗");
            return [];
        }
    }

    // 🆕 讀取歷史記錄檔案
    public static SavedRecordModel LoadSavedRecord(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<SavedRecordModel>(json, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "讀取歷史記錄檔案失敗: {FilePath}", filePath);
            return null;
        }
    }

    public void UpdateItemInfo(ItemBaseModel item)
    {
        Log.Debug("更新尚未存檔的拾取物品資訊: {ItemId}", item.Id);
        if (_currentMapPickData.TryGetValue(item.Id, out var pickedItem))
        {
            pickedItem.Name = item.Name;
        }

        foreach (var mapRecord in _mapRecords)
        {
            if (mapRecord.PickRecord != null && mapRecord.PickRecord.TryGetValue(item.Id, out var pickedItemInRecord))
            {
                pickedItemInRecord.Name = item.Name;
            }
        }        
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

/// <summary>
/// 自動存檔的資料模型
/// </summary>
public class SavedRecordModel
{
    public RecordSummary Summary { get; set; }
    public List<MapRecordModel> Records { get; set; }
    public DateTime SavedTime { get; set; }
}

/// <summary>
/// 統計摘要
/// </summary>
public class RecordSummary
{
    public int TotalMaps { get; set; }
    public int TotalItems { get; set; }
    public int TotalQuantity { get; set; }
    public string TotalPlayTime { get; set; }
    public List<TopPickedItem> MostPickedItems { get; set; }
}

/// <summary>
/// 最受歡迎的拾取物品
/// </summary>
public class TopPickedItem
{
    public int BaseId { get; set; }
    public string Name { get; set; }
    public int TotalQuantity { get; set; }
    public int Like { get; set; }
}
