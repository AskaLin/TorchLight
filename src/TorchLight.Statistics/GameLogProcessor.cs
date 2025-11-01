using Serilog;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Models;
using TorchLight.Statistics.Services;

namespace TorchLight.Statistics;

/// <summary>
/// 遊戲日誌處理器 - 整合所有日誌處理邏輯
/// </summary>
public class GameLogProcessor
{
    private readonly LineParser _lineParser;
    private readonly ItemChangeBlockProcessor _itemChangeProcessor;
    private readonly BagInventoryManager _bagInventoryManager;
    private readonly MapPickRecordManager _mapPickRecordManager;
    private readonly ConsoleLogger _logger;
    private readonly Dictionary<int, ItemModel> _itemTable;
    private WebViewHub _webViewHub;
    // 🆕 SafeFileTailWatcher 引用
    private SafeFileTailWatcher _fileTailWatcher;

    /// <summary>
    /// 當檢測到 "已開啟日誌" 訊息時觸發
    /// </summary>

    public event Action OnLogOpenedDetected;

    /// <summary>
    /// 當背包同步完成時觸發
    /// </summary>
    public event Action OnBagSyncCompleted;


    public GameLogProcessor(
       Dictionary<int, ItemModel> itemTable,
       LineParser lineParser,
       ItemChangeBlockProcessor itemChangeProcessor,
       WebViewHub webViewHub = null)
    {
        _lineParser = lineParser ?? throw new ArgumentNullException(nameof(lineParser));
        _itemChangeProcessor = itemChangeProcessor ?? throw new ArgumentNullException(nameof(itemChangeProcessor));
        _itemTable = itemTable ?? throw new ArgumentNullException(nameof(itemTable));
        _webViewHub = webViewHub;
        _bagInventoryManager = new BagInventoryManager(itemTable);
        _mapPickRecordManager = new MapPickRecordManager(itemTable); ;
        _logger = new ConsoleLogger();

        // 註冊事件處理
        _itemChangeProcessor.OnBagModInsideBlock += HandleBagModification;
    }

    /// <summary>
    /// 設定 WebViewHub（用於後續通知前端）
    /// </summary>
    public void SetWebViewHub(WebViewHub webViewHub)
    {
        _webViewHub = webViewHub;
    }

    /// <summary>
    /// 🆕 設定 SafeFileTailWatcher（用於控制檔案大小監控）
    /// </summary>
    public void SetFileTailWatcher(SafeFileTailWatcher fileTailWatcher)
    {
        _fileTailWatcher = fileTailWatcher;
    }


    /// <summary>
    /// 處理遊戲日誌
    /// </summary>
    public void ProcessLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        try
        {

            // 開始開新圖, 結算舊圖
            if (LineParser.OpenMap(line, out var datetime))
            {
                _mapPickRecordManager.EndMapRecord(datetime);
                // 通知前端：地圖結算完成，新記錄已產生
                NotifyNewMapRecord();
                return;
            }

            // 搭配開圖, 取得地圖 Token
            if (LineParser.IsTokenLine(line, out string mapToken, !_mapPickRecordManager.CurrentMapRecordInfoComplete()))
            {
                _mapPickRecordManager.SetMapToken(mapToken);
                return;
            }

            if (LineParser.IsCurrentLevelLine(line, out int mapLevel, _mapPickRecordManager.CurrentMapRecordInfoComplete()))
            {
                _mapPickRecordManager.SetMapLevel(mapLevel);
                return;
            }

            if (LineParser.IsCurrentOpenMapIDLine(line, out int mapId, _mapPickRecordManager.CurrentMapRecordInfoComplete()))
            {
                _mapPickRecordManager.SetMapId(mapId);
                return;
            }

            // 0. 檢查 "已開啟日誌" 訊息
            if (LineParser.IsLogOpenedMessage(line))
            {
                Log.Information("檢測到 '已開啟日誌' 訊息");

                // 🆕 啟用檔案大小監控
                _fileTailWatcher?.EnableLogMonitoring();

                OnLogOpenedDetected?.Invoke();
                return;
            }

            // 1. 檢查背包初始化狀態
            var (isInitLine, shouldProcess, isComplete, isFirstInit) = _lineParser.CheckBagInitializationState(line);

            if (isInitLine && shouldProcess)
            {
                // 只在第一次開始初始化時才重置背包
                if (isFirstInit)
                {
                    Log.Information("偵測到背包初始化，重置背包資料");
                    _bagInventoryManager.Reset();
                }

                // 處理初始化背包物品
                var itemData = _lineParser.GetItemData(line);
                _bagInventoryManager.InitializeBagItem(itemData);
                Log.Debug("初始化背包物品: {ItemName} x{Count}", itemData.Name, itemData.Num);
                return;
            }

            if (isComplete)
            {
                // 初始化完成
                Log.Information("背包初始化完成，共 {Count} 種物品", _bagInventoryManager.BagData.Count);
                _bagInventoryManager.PrintInitializedBag();
                OnBagSyncCompleted?.Invoke();
                return;
            }

            // 2. 登入開始 - 重置所有資料
            if (LineParser.IsLoginStart(line))
            {
                Log.Information("偵測到重新登入，重置所有資料");
                _bagInventoryManager.Reset();
                _mapPickRecordManager.Reset();
                _lineParser.ResetInitializationState();

                // 🆕 停用檔案大小監控（等待下次"已開啟日誌"訊息）
                _fileTailWatcher?.DisableLogMonitoring();

                return;
            }

            // 3. 地圖切換
            if (LineParser.IsMoveMap(line))
            {
                var (time, fromPath, toPath, success) = LineParser.GetMapPathData(line);
                if (success)
                {
                    var fromMapInfo = MapInfoMapper.GetMapInfo(fromPath);
                    var toMapInfo = MapInfoMapper.GetMapInfo(toPath);

                    Log.Debug("地圖切換: {From} -> {To}", fromPath, toPath);
                    Log.Information($"{time:yyyy/MM/dd HH:mm:ss}\t從地圖 {fromMapInfo.Name} 進入地圖 {toMapInfo.Name}");

                    // 從藏身處進入異界地圖, 開啟地圖拾取紀錄
                    if (fromMapInfo.Type == MapType.Hideout && toMapInfo.Type != MapType.Hideout)
                    {
                        _mapPickRecordManager.StartMapRecord(toMapInfo, time);
                    }

                    // 通知前端地圖切換
                    if (_webViewHub != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            await _webViewHub.NotifyCurrentMapUpdateAsync(GetCurrentMapData());
                        });
                    }
                }
                return;
            }

            // 4. 處理物品變更（區塊處理）
            _itemChangeProcessor.HandleLine(line);

            // 0. 遊戲關閉
            if (LineParser.CloseGame(line))
            {
                Log.Information("偵測到遊戲關閉, 結算關卡資料");
                NotifyNewMapRecord();
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "處理日誌行時發生錯誤，日誌內容: {Line}", line);
        }

        void NotifyNewMapRecord()
        {
            if (_webViewHub != null)
            {
                _ = Task.Run(async () =>
                {
                    await _webViewHub.NotifyNewMapRecordAsync();
                    await _webViewHub.NotifyCurrentMapUpdateAsync(GetCurrentMapData());
                });
            }
        }
    }

    /// <summary>
    /// 處理背包物品修改事件
    /// </summary>
    private void HandleBagModification(BagModEvent ev)
    {
        try
        {
            // 更新背包庫存
            var bagResult = _bagInventoryManager.UpdateBagItem(ev);

            // 記錄日誌
            _logger.LogBagModification(ev, bagResult);

            // 處理 Spv3Open 事件（開圖材料）
            if (ev.ProtoName == "Spv3Open" && _itemTable.TryGetValue(ev.ConfigBaseId, out var item))
            {
                if (item.Type == ItemType.Currency)
                {
                    Log.Debug("[開圖材料] 使用迴響數量 {res}", Math.Abs(bagResult.QuantityChange));
                }

                // 記錄羅盤、探針和門票作為開圖材料
                if (item.Type == ItemType.Compass || item.Type == ItemType.Probe ||
                    item.Type == ItemType.MapTicket || item.Type == ItemType.BossTicket ||
                    item.Type == ItemType.GameplayTicket || item.Type == ItemType.Currency)
                {
                    _mapPickRecordManager.RecordMapMaterial(ev.ConfigBaseId, item.Type);
                }
            }

            // 如果是增加物品（拾取），且在異界地圖中，則記錄拾取
            if (bagResult.QuantityChange > 0 && ev.ProtoName == "PickItems")
            {
                var mapResult = _mapPickRecordManager.RecordPickedItem(ev.ConfigBaseId, ev.SlotId, bagResult.QuantityChange);
                if (mapResult != null)
                {
                    _logger.LogMapPickItem(_mapPickRecordManager.CurrentMapName, mapResult);

                    // 通知前端物品拾取
                    if (_webViewHub != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            await _webViewHub.NotifyItemPickedAsync(mapResult.ItemName, mapResult.QuantityChange);
                            await _webViewHub.NotifyCurrentMapUpdateAsync(GetCurrentMapData());
                        });
                    }
                }
            }

            // 背包同步完成事件
            OnBagSyncCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "處理背包修改時發生錯誤");
        }
    }

    /// <summary>
    /// 獲取當前地圖資料（用於通知前端）
    /// </summary>
    public MapRecordViewModel GetCurrentMapData()
    {
        try
        {
            var currentRecord = _mapPickRecordManager.GetCurrentMapRecord();

            if (!_mapPickRecordManager.IsInMap)
            {
                // 避難所地圖
                return new MapRecordViewModel(false, MapType.Hideout, _mapPickRecordManager.CurrentMapName);
            }
            else if (currentRecord != null)
            {
                // ✅ 即時從 MapInfoMapper 獲取最新的地圖名稱
                var latestMapName = MapInfoMapper.GetMapName(currentRecord.Id);
                var mapInfo = new MapInfo()
                {
                    Id = currentRecord.Id,
                    Name = latestMapName,
                    Type = currentRecord.Type
                };

                return new MapRecordViewModel
                {
                    IsInMap = true,
                    MapType = MapType.Netherrealm.ToString(),
                    MapName = mapInfo.RealName(currentRecord.MapTicketId),
                    RecordId = currentRecord.RecordId,
                    MapTicket = currentRecord.MapTicket,
                    Compass = currentRecord.Compass,
                    Probe = currentRecord.Probe,
                    StartTime = currentRecord.StartTime,
                    Items = currentRecord.PickRecord?.Select(p => new PickedItemViewModel
                    {
                        BaseId = p.Value.BaseId,
                        Name = p.Value.Name,
                        Total = p.Value.Total,
                        Slots = p.Value.Slots
                    }).OrderByDescending(i => i.Total).ToArray() ?? []
                };
            }
            else
            {
                // 在異界地圖但沒有記錄
                Log.Warning("在異界地圖但沒有記錄");
                return new MapRecordViewModel(true, MapType.Netherrealm, _mapPickRecordManager.CurrentMapName);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取當前地圖資料失敗");
            return null;
        }
    }

    /// <summary>
    /// 獲取背包管理器（用於測試或外部存取）
    /// </summary>
    public BagInventoryManager BagInventoryManager => _bagInventoryManager;

    /// <summary>
    /// 獲取地圖記錄管理器（用於測試或外部存取）
    /// </summary>
    public MapPickRecordManager MapPickRecordManager => _mapPickRecordManager;
}