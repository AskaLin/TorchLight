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
    private WebViewNotificationThrottle _notificationThrottle; // 🆕 通知節流器
    private SafeFileTailWatcher _fileTailWatcher;

    // 🆕 處理器鏈（按優先級排序）
    private readonly List<BaseLogProcessor> _processorChain;
    private readonly InitBagProcessor _initBagProcessor;
    private readonly PickedItemProcessor _pickedItemProcessor;
    private readonly PushItemProcessor _pushItemProcessor;
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

        // 🆕 初始化通知節流器
        if (_webViewHub != null)
        {
            _notificationThrottle = new WebViewNotificationThrottle(_webViewHub);
        }

        // 初始化處理器
        _initBagProcessor = new InitBagProcessor();
        _pickedItemProcessor = new PickedItemProcessor();
        _pushItemProcessor = new PushItemProcessor();

        _openMapProcessor = new OpenMapProcessor();
        _openSeasonMapProcessor = new OpenSeasonMapProcessor();

        // 🆕 建立處理器鏈（優先級由高到低）
        _processorChain =
        [
            _initBagProcessor,           // 1. 背包初始化（最高優先級）
            _pickedItemProcessor,        // 2. 拾取物品
            _pushItemProcessor,          // 3. 推送物品
            _openMapProcessor,           // 4. 開啟地圖
            _openSeasonMapProcessor      // 5. 開啟賽季地圖
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

        _pushItemProcessor.OnItemsPushed += (ev) =>
        {
            // 更新背包庫存
            var bagResult = UpdateBagInventory(ev);
        };

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
        
        // 🆕 初始化通知節流器
        _notificationThrottle?.Dispose();
        _notificationThrottle = new WebViewNotificationThrottle(_webViewHub);
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
        // 回到避難所
        if (line.Contains("[Game] UGameMgr::EnterLevel(110) mode=1 reload=0."))
        {
            // 記錄回來的時間, 可能是中離也可能是完成            
            _mapPickRecordManager.ReturnTime = LineParser.GetLineDateTime(line);
            Log.Information($"偵測到返回避難所, 紀錄返回時間 {_mapPickRecordManager.ReturnTime:HH:mm:ss.fff}");
            // 不需要調用 SetIsInMap，因為在 EndMapRecord 時會自動設定 IsInMap = false
            NotifyCurrentMapUpdate();
        }

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

        // 遊戲關閉 - 標記當前地圖為未完成
        if (LineParser.CloseGame(line))
        {
            Log.Information("偵測到遊戲關閉");
            if (_mapPickRecordManager.IsInMap)
            {
                _mapPickRecordManager.MarkCurrentMapAsIncomplete();
            }
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
        if (_notificationThrottle != null)
        {
            _ = Task.Run(async () =>
            {
                // 新地圖記錄立即發送（不節流）
                await _notificationThrottle.NotifyNewMapRecordAsync();
                
                // 地圖更新使用節流
                _notificationThrottle.NotifyCurrentMapUpdate(GetCurrentMapData());
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

            MapRecordViewModel mapRecord = currentRecord != null ? new MapRecordViewModel
            {
                IsInMap = _mapPickRecordManager.IsInMap,
                MapType = currentRecord.Type.ToString(),
                MapName = $"{currentRecord.Name} ({currentRecord.MapId})",
                MapId = currentRecord.MapId, // 🆕 添加 MapId
                Resonance = currentRecord.Resonance,
                RecordId = currentRecord.RecordId,
                MapTicket = currentRecord.MapTicket,
                Compass = currentRecord.Compass,
                Probe = currentRecord.Probe,
                StartTime = currentRecord.StartTime,
                // ✅ 修正：包含 Like 值並按 Like => Total 排序
                Items = currentRecord.PickRecord?.Select(p =>
                {
                    return new PickedItemViewModel
                    {
                        BaseId = p.Value.BaseId,
                        Name = p.Value.Name,
                        Total = p.Value.Total,
                        Slots = p.Value.Slots,
                        Like = p.Value.Like,    // ✅ 包含 Like 值
                        ItemType = p.Value.ItemType, // ✅ 包含 ItemType
                        PageId = p.Value.PageId      // ✅ 包含 PageId
                    };
                })
                .OrderByDescending(i => i.Like)  // ✅ 先按 Like 排序
                .ThenByDescending(i => i.Total)    // ✅ 再按數量排序
                .ToArray() ?? []
            } : null;


            if (!_mapPickRecordManager.IsInMap)
            {
                if(mapRecord == null)
                {
                    return new MapRecordViewModel(false, MapType.Hideout, "");
                }
                else
                {
                    mapRecord.IsIncomplete = true;
                    return mapRecord;
                }
                
            }
            else if (mapRecord != null)
            {
                return mapRecord;
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
        
        // 🆕 使用節流器發送背包同步通知（防抖動）
        if (_notificationThrottle != null)
        {
            _notificationThrottle.NotifyBagSync();
        }
        
        OnBagSyncCompleted?.Invoke();
    }

    private ItemChangeResult UpdateBagInventory(ItemChangeEvent ev)
    {
        // 更新背包庫存
        ItemChangeResult bagResult = _bagInventoryManager.UpdateBagItem(ev);

        // 記錄日誌
        _logger.LogBagModification(ev, bagResult);

        return bagResult;
    }

    /// <summary>
    /// 處理背包物品修改事件
    /// </summary>
    private void HandleBagModification(ItemChangeEvent ev)
    {
        try
        {
            // 更新背包庫存
            var bagResult = UpdateBagInventory(ev);

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

                    // 🆕 使用節流器發送通知（批次處理 + 防抖動）
                    if (_notificationThrottle != null)
                    {
                        // 物品拾取通知（批次處理）
                        _notificationThrottle.NotifyItemPicked(mapResult.ItemName, mapResult.QuantityChange);
                        
                        // 地圖更新通知（防抖動）
                        _notificationThrottle.NotifyCurrentMapUpdate(GetCurrentMapData());
                        
                        // 🆕 背包同步通知（防抖動）
                        _notificationThrottle.NotifyBagSync();
                    }
                }
            }
            else
            {
                // 🆕 非拾取事件（如使用、丟棄等）也觸發背包同步
                if (_notificationThrottle != null)
                {
                    _notificationThrottle.NotifyBagSync();
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
        DateTime newMapStartTime = start;
        // 開始新的地圖記錄, 用返回避難所的時間做為結束時間
        if (_mapPickRecordManager.ReturnTime != DateTime.MinValue)
        {
            Log.Information($"使用返回避難所時間 {_mapPickRecordManager.ReturnTime:HH:mm:ss.fff} 作為上一張地圖的結束時間");
            newMapStartTime = _mapPickRecordManager.ReturnTime;
        }
        else
        {
            Log.Information("沒有返回避難所時間, 使用新地圖開始時間作為上一張地圖的結束時間");
        }

        _mapPickRecordManager.EndMapRecord(newMapStartTime);
        NotifyNewMapRecord();
        _mapPickRecordManager.ReturnTime = DateTime.MinValue;
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
        if (_notificationThrottle != null)
        {
            // 🆕 使用防抖動
            _notificationThrottle.NotifyCurrentMapUpdate(GetCurrentMapData());
        }
    }
    #endregion
}