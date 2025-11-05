using Serilog;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Models;
using TorchLight.Statistics.Services;

namespace TorchLight.Statistics.LogProcessor;

/// <summary>
/// 遊戲日誌處理器 - 整合所有日誌處理邏輯（責任鏈模式）
/// </summary>
public class GameLogProcessor
{
    private readonly BagInventoryManager _bagInventoryManager;
    private readonly MapPickRecordManager _mapPickRecordManager;
    private readonly ConsoleLogger _logger;
    private WebViewHub _webViewHub;
    private SafeFileTailWatcher _fileTailWatcher;

    // 🆕 處理器鏈（按優先級排序）
    private readonly List<BaseLogProcessor> _processorChain;
    private readonly InitBagProcessor _initBagProcessor;
    private readonly PickedItemProcessor _pickedItemProcessor;
    private readonly OpenMapProcessor _openMapProcessor;
    private readonly OpenSeasonMapProcessor _openSeasonMapProcessor;

    /// <summary>
    /// 當檢測到 "已開啟日誌" 訊息時觸發
    /// </summary>
    public event Action OnLogOpenedDetected;

    /// <summary>
    /// 當背包同步完成時觸發
    /// </summary>
    public event Action OnBagSyncCompleted;

    public GameLogProcessor(WebViewHub webViewHub = null)
    {
        _webViewHub = webViewHub;
        _bagInventoryManager = new BagInventoryManager();
        _mapPickRecordManager = new MapPickRecordManager();
        _logger = new ConsoleLogger();

        // 初始化處理器
        _initBagProcessor = new InitBagProcessor();
        _pickedItemProcessor = new PickedItemProcessor();
        _openMapProcessor = new OpenMapProcessor();
        _openSeasonMapProcessor = new OpenSeasonMapProcessor();

        // 🆕 建立處理器鏈（優先級由高到低）
        _processorChain =
        [
            _initBagProcessor,           // 1. 背包初始化（最高優先級）
            _pickedItemProcessor,        // 2. 拾取物品
            _openMapProcessor,           // 3. 開啟地圖
            _openSeasonMapProcessor      // 4. 開啟賽季地圖
        ];

        RegisterEventHandlers();
    }

    private void RegisterEventHandlers()
    {
        // 背包初始化事件
        _initBagProcessor.OnInitStarted += HandleInitStart;
        _initBagProcessor.OnItemInitialized += HandleItemInitialized;
        _initBagProcessor.OnInitCompleted += HandleInitCompleted;

        // 拾取物品事件
        _pickedItemProcessor.OnItemsPicked += HandleBagModification;

        // 開啟地圖事件
        _openMapProcessor.OnMapStart += HandleMapInfoStart;
        _openMapProcessor.OnMapComplete += HandleMapInfoComplete;
        _openMapProcessor.OnItemChangeInMapBlock += HandleBagModification;

        // 開啟賽季地圖事件
        _openSeasonMapProcessor.OnMapStart += HandleMapInfoStart;
        _openSeasonMapProcessor.OnMapComplete += HandleMapInfoComplete;
    }

    /// <summary>
    /// 設定 WebViewHub（用於後續通知前端）
    /// </summary>
    public void SetWebViewHub(WebViewHub webViewHub)
    {
        _webViewHub = webViewHub;
    }

    /// <summary>
    /// 設定 SafeFileTailWatcher（用於控制檔案大小監控）
    /// </summary>
    public void SetFileTailWatcher(SafeFileTailWatcher fileTailWatcher)
    {
        _fileTailWatcher = fileTailWatcher;
    }

    /// <summary>
    /// 處理遊戲日誌（責任鏈模式）
    /// </summary>
    public void ProcessLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        try
        {
            // 🆕 使用處理器鏈處理日誌（任一處理器處理後即返回）
            foreach (var processor in _processorChain)
            {
                if (processor.HandleLine(line))
                {
                    return; // 已處理，結束
                }
            }

            // 處理全域事件（不在區塊中的日誌）
            ProcessGlobalEvents(line);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "處理日誌行時發生錯誤，日誌內容: {Line}", line);
        }
    }

    /// <summary>
    /// 🆕 處理全域事件（不屬於任何處理器的日誌）
    /// </summary>
    private void ProcessGlobalEvents(string line)
    {
        // 檢查 "已開啟日誌" 訊息
        if (LineParser.IsLogOpenedMessage(line))
        {
            Log.Information("檢測到 '已開啟日誌' 訊息");
            _fileTailWatcher?.EnableLogMonitoring();
            OnLogOpenedDetected?.Invoke();
            return;
        }

        // 登入開始 - 重置所有資料
        if (LineParser.IsLoginStart(line))
        {
            Log.Information("偵測到重新登入，重置所有資料");
            ResetAllProcessors();
            _fileTailWatcher?.DisableLogMonitoring();
            return;
        }

        // 遊戲關閉
        if (LineParser.CloseGame(line))
        {
            Log.Information("偵測到遊戲關閉, 結算關卡資料");
            NotifyNewMapRecord();
        }
    }

    /// <summary>
    /// 🆕 重置所有處理器
    /// </summary>
    private void ResetAllProcessors()
    {
        _bagInventoryManager.Reset();
        _mapPickRecordManager.Reset();

        foreach (var processor in _processorChain)
        {
            processor.Reset();
        }
    }

    private void NotifyNewMapRecord()
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
                return new MapRecordViewModel(false, MapType.Hideout, _mapPickRecordManager.CurrentMapName);
            }
            else if (currentRecord != null)
            {
                return new MapRecordViewModel
                {
                    IsInMap = true,
                    MapType = currentRecord.Type.ToString(),
                    MapName = $"{currentRecord.Name} ({currentRecord.MapId})",
                    MapId = currentRecord.MapId, // 🆕 添加 MapId
                    Resonance = currentRecord.Resonance,
                    RecordId = currentRecord.RecordId,
                    MapTicket = currentRecord.MapTicket,
                    Compass = currentRecord.Compass,
                    Probe = currentRecord.Probe,
                    StartTime = currentRecord.StartTime,
                    Items = currentRecord.PickRecord?.Select(p =>
                    {
                        var itemInfo = ItemInfoMapper.GetItemInfo(p.Value.BaseId);
                        string itemType = "Unknown";
                        int pageId = 0;
                        if (itemInfo != null)
                        {
                            itemType = itemInfo.Type.ToString();
                            pageId = (int)itemInfo.PageIdType;
                        }
                        return new PickedItemViewModel
                        {
                            BaseId = p.Value.BaseId,
                            Name = p.Value.Name,
                            Total = p.Value.Total,
                            Slots = p.Value.Slots,
                            ItemType = itemType,
                            PageId = pageId
                        };
                    }).OrderByDescending(i => i.Total).ToArray() ?? []
                };
            }
            else
            {
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

    #region 事件處理方法

    /// <summary>
    /// 處理背包初始化開始
    /// </summary>
    private void HandleInitStart(DateTime startTime)
    {
        Log.Information("偵測到背包初始化，重置背包資料");
        _bagInventoryManager.Reset();
    }

    /// <summary>
    /// 處理背包物品初始化
    /// </summary>
    private void HandleItemInitialized(ItemModel item)
    {
        // 可選：即時處理初始化物品
    }

    /// <summary>
    /// 處理背包初始化完成
    /// </summary>
    private void HandleInitCompleted(InitBagEvent initEvent)
    {
        foreach (var item in initEvent.Items)
        {
            _bagInventoryManager.InitializeBagItem(item);
        }

        _bagInventoryManager.PrintInitializedBag();
        OnBagSyncCompleted?.Invoke();
    }

    /// <summary>
    /// 處理背包物品修改事件
    /// </summary>
    private void HandleBagModification(ItemChangeEvent ev)
    {
        try
        {
            // 更新背包庫存
            var bagResult = _bagInventoryManager.UpdateBagItem(ev);

            // 記錄日誌
            _logger.LogBagModification(ev, bagResult);

            // 紀錄未知物品            
            if (!ItemInfoMapper.TryGetItemInfo(ev.ConfigBaseId, out var item))
            {
                item = new()
                {
                    Id = ev.ConfigBaseId,
                    Name = $"未知物件({ev.ConfigBaseId})",
                    Type = ev.PageId switch
                    {
                        100 => ItemType.Unknown100,
                        101 => ItemType.Unknown101,
                        102 => ItemType.Unknown102,
                        103 => ItemType.Unknown103,
                        _ => ItemType.Unknown
                    },
                    PageIdType = ev.PageId switch
                    {
                        100 => PageIdType.Equipment,
                        101 => PageIdType.Skill,
                        102 => PageIdType.Currency,
                        103 => PageIdType.Other,
                        _ => PageIdType.Other
                    }
                };

                ItemInfoMapper.AddOrUpdateItem(item);
            }

            // 處理 Spv3Open 事件（開圖材料）
            if (ev.ProtoName == "Spv3Open")
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
                    _mapPickRecordManager.RecordMapMaterial(item, Math.Abs(bagResult.QuantityChange));
                }
            }

            // 如果是增加物品（拾取），且在異界地圖中，則記錄拾取
            if (bagResult.QuantityChange > 0 && (ev.ProtoName == "PickItems" || ev.ProtoName == "PickItem"))
            {
                var mapResult = _mapPickRecordManager.RecordPickedItem(item.Name, ev.ConfigBaseId, ev.PageId, ev.SlotId, bagResult.QuantityChange);
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

    private void HandleMapInfoStart(DateTime start)
    {
        _mapPickRecordManager.EndMapRecord(start);
        NotifyNewMapRecord();
    }

    /// <summary>
    /// 處理地圖資訊收集完成事件
    /// </summary>
    private void HandleMapInfoComplete(OpenMapEvent context)
    {
        try
        {
            _mapPickRecordManager.SetMapToken(context.Token);
            _mapPickRecordManager.SetMapId(context.MapId);

            if (_mapPickRecordManager.CurrentMapRecordInfoComplete())
            {
                _mapPickRecordManager.StartMapRecord(context.StartTime);
                NotifyCurrentMapUpdate();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "處理地圖資訊完成時發生錯誤");
        }
    }

    public void UpdateMapInfo(List<int> mapIds)
    {
        _mapPickRecordManager.UpdateMapInfo(mapIds);
        NotifyCurrentMapUpdate();
    }

    public void UpdateItemInfo(ItemBaseModel item)
    {
        _mapPickRecordManager.UpdateItemInfo(item);
        NotifyCurrentMapUpdate();
    }

    private void NotifyCurrentMapUpdate()
    {
        if (_webViewHub != null)
        {
            _ = Task.Run(async () =>
            {
                await _webViewHub.NotifyCurrentMapUpdateAsync(GetCurrentMapData());
            });
        }
    }
    #endregion
}