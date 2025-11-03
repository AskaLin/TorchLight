using Serilog;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Models;
using TorchLight.Statistics.Services;

namespace TorchLight.Statistics.LogProcessor;

/// <summary>
/// 遊戲日誌處理器 - 整合所有日誌處理邏輯
/// </summary>
public class GameLogProcessor
{    
    private readonly BagInventoryManager _bagInventoryManager;
    private readonly MapPickRecordManager _mapPickRecordManager;
    private readonly ConsoleLogger _logger;
    private readonly Dictionary<int, ItemModel> _itemTable;
    private WebViewHub _webViewHub;
    // 🆕 SafeFileTailWatcher 引用
    private SafeFileTailWatcher _fileTailWatcher;

    private readonly ItemChangeProcessor _itemChangeProcessor;
    private readonly OpenMapProcessor _openMapProcessor;
    private readonly InitBagProcessor _initBagProcessor;

    /// <summary>
    /// 當檢測到 "已開啟日誌" 訊息時觸發
    /// </summary>

    public event Action OnLogOpenedDetected;

    /// <summary>
    /// 當背包同步完成時觸發
    /// </summary>
    public event Action OnBagSyncCompleted;


    public GameLogProcessor(WebViewHub webViewHub = null)    {
        
        _itemTable = ItemInfoMapper.GetItemTable();
        _webViewHub = webViewHub;
        _bagInventoryManager = new BagInventoryManager(_itemTable);
        _mapPickRecordManager = new MapPickRecordManager(_itemTable);
        _logger = new ConsoleLogger();


        _itemChangeProcessor = new ItemChangeProcessor();
        _openMapProcessor = new OpenMapProcessor();
        _initBagProcessor = new InitBagProcessor();


        // 註冊事件處理
        _itemChangeProcessor.OnItemChanged += HandleBagModification;

        _openMapProcessor.OnMapStart += HandleMapInfoStart;
        _openMapProcessor.OnMapComplete += HandleMapInfoComplete;

        _initBagProcessor.OnInitStarted += HandleInitStart;
        _initBagProcessor.OnItemInitialized += HandleItemInitialized;
        _initBagProcessor.OnInitCompleted += HandleInitCompleted;

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
            // 0. 檢查 "已開啟日誌" 訊息
            if (LineParser.IsLogOpenedMessage(line))
            {
                Log.Information("檢測到 '已開啟日誌' 訊息");

                // 🆕 啟用檔案大小監控
                _fileTailWatcher?.EnableLogMonitoring();

                OnLogOpenedDetected?.Invoke();
                return;
            }

            // 1. 處理背包初始化（使用 InitBagProcessor）
            _initBagProcessor.HandleLine(line);

            // 2. 登入開始 - 重置所有資料
            if (LineParser.IsLoginStart(line))
            {
                Log.Information("偵測到重新登入，重置所有資料");
                _bagInventoryManager.Reset();
                _mapPickRecordManager.Reset();
                _initBagProcessor.Reset();

                // 🆕 停用檔案大小監控（等待下次"已開啟日誌"訊息）
                _fileTailWatcher?.DisableLogMonitoring();

                return;
            }


            // 0. 遊戲關閉
            if (LineParser.CloseGame(line))
            {
                Log.Information("偵測到遊戲關閉, 結算關卡資料");
                NotifyNewMapRecord();
                return;
            }

            // 🆕 處理開啟地圖區塊（在檢查地圖資訊之前）
            _openMapProcessor.HandleLine(line);

            // 4. 處理物品變更（區塊處理）
            _itemChangeProcessor.HandleLine(line);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "處理日誌行時發生錯誤，日誌內容: {Line}", line);
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
            if (bagResult.QuantityChange > 0 && (ev.ProtoName == "PickItems" || ev.ProtoName == "PickItem"))
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
                return new MapRecordViewModel
                {
                    IsInMap = true,
                    MapType = MapType.Netherrealm.ToString(),
                    MapName = currentRecord.Name,
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
        //_bagInventoryManager.InitializeBagItem(item);
        //Log.Debug("初始化背包物品: {ItemName} x{Count}", item.Name, item.Num);
    }

    /// <summary>
    /// 處理背包初始化完成
    /// </summary>
    private void HandleInitCompleted(InitBagEvent initEvent)
    {
        foreach(var item in initEvent.Items)
        {
            _bagInventoryManager.InitializeBagItem(item);
        }
        
        _bagInventoryManager.PrintInitializedBag();
        OnBagSyncCompleted?.Invoke();
    }

    private void HandleMapInfoStart(DateTime start)
    {
        _mapPickRecordManager.EndMapRecord(start);
        // 通知前端：地圖結算完成，新記錄已產生
        NotifyNewMapRecord();
    }

    /// <summary>
    /// 🆕 處理地圖資訊收集完成事件
    /// </summary>
    private void HandleMapInfoComplete(OpenMapEvent context)
    {
        try
        {
            _mapPickRecordManager.SetMapToken(context.Token);
            _mapPickRecordManager.SetMapId(context.MapId);
            _mapPickRecordManager.SetMapLevel(context.Level);

            if (_mapPickRecordManager.CurrentMapRecordInfoComplete())
            {
                _mapPickRecordManager.StartMapRecord(context.StartTime);

                // 通知前端地圖切換
                if (_webViewHub != null)
                {
                    _ = Task.Run(async () =>
                    {
                        await _webViewHub.NotifyCurrentMapUpdateAsync(GetCurrentMapData());
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "處理地圖資訊完成時發生錯誤");
        }
    }

    #endregion
}