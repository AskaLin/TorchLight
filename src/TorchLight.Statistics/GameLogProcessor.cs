using Serilog;
using System.Text.RegularExpressions;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Models;
using TorchLight.Statistics.Services;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

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
    private readonly MapTransitionHandler _mapTransitionHandler;
    private readonly ConsoleLogger _logger;
    private readonly Dictionary<int, ItemModel> _itemTable;
    private WebViewHub? _webViewHub;

    /// <summary>
    /// 當檢測到 "已開啟日誌" 訊息時觸發
    /// </summary>
#nullable enable
    public event Action? OnLogOpenedDetected;

    /// <summary>
    /// 當背包同步完成時觸發
    /// </summary>
    public event Action? OnBagSyncCompleted;
#nullable disable

    public GameLogProcessor(
       Dictionary<int, ItemModel> itemTable,
    LineParser lineParser,
           ItemChangeBlockProcessor itemChangeProcessor,
           WebViewHub? webViewHub = null)
    {
        _lineParser = lineParser ?? throw new ArgumentNullException(nameof(lineParser));
        _itemChangeProcessor = itemChangeProcessor ?? throw new ArgumentNullException(nameof(itemChangeProcessor));
        _itemTable = itemTable ?? throw new ArgumentNullException(nameof(itemTable));
        _webViewHub = webViewHub;
        _bagInventoryManager = new BagInventoryManager(itemTable);
        _mapPickRecordManager = new MapPickRecordManager(itemTable);
        _mapTransitionHandler = new MapTransitionHandler(_mapPickRecordManager);
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

        // 同時設定給 MapTransitionHandler
        _mapTransitionHandler.SetWebViewHub(webViewHub);
    }

    private bool SPV3OPENStart = false;
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
            if (line.Contains("----Socket RecvMessage STT----Spv3Open----"))
            {
                Log.Debug("開始開新圖, 結算舊圖");
                var match = Regex.Match(line, @"\[(\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\]");
                if (match.Success)
                {
                    SPV3OPENStart = true;
                    _mapPickRecordManager.EndMapRecord(LineParser.ParseUnrealDateTime(match.Groups[1].Value));
                }

                return;
            }

            if (line.Contains("TokenKey"))
            {
                var match = Regex.Match(line, @"\[(\d+)\]");

                if (match.Success)
                {
                    string token = match.Groups[1].Value;
                    _mapPickRecordManager.SetMapToken(token);                    
                    SPV3OPENStart = false;
                }
            }



            // 0. 檢查 "已開啟日誌" 訊息
            if (_lineParser.IsLogOpenedMessage(line))
            {
                Log.Information("檢測到 '已開啟日誌' 訊息");
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
            if (_lineParser.IsLoginStart(line))
            {
                Log.Information("偵測到重新登入，重置所有資料");
                _bagInventoryManager.Reset();
                _mapPickRecordManager.Reset();
                _lineParser.ResetInitializationState();
                return;
            }

            // 3. 地圖切換
            if (_lineParser.IsMoveMap(line))
            {
                var (time, fromPath, toPath, success) = _lineParser.GetMapPathData(line);
                if (success)
                {
                    Log.Debug("地圖切換: {From} -> {To}", fromPath, toPath);
                    _mapTransitionHandler.HandleMapTransition(time, fromPath, toPath);

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
        }
        catch (Exception ex)
        {
            Log.Error(ex, "處理日誌行時發生錯誤，日誌內容: {Line}", line);
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
    private object? GetCurrentMapData()
    {
        try
        {
            var currentRecord = _mapPickRecordManager.GetCurrentMapRecord();

            if (!_mapPickRecordManager.IsInNetherrealmMap)
            {
                // 避難所地圖
                return new
                {
                    IsInMap = false,
                    MapType = "Hideout",
                    MapName = _mapPickRecordManager.CurrentMapName,
                    RecordId = (Guid?)null,
                    MapTicket = "",
                    Compass = Array.Empty<string>(),
                    Probe = "",
                    StartTime = (DateTime?)null,
                    Items = Array.Empty<object>()
                };
            }
            else if (currentRecord != null)
            {
                // 異界地圖 - 即時從 MapInfoMapper 獲取最新名稱
                return new
                {
                    IsInMap = true,
                    MapType = "Netherrealm",
                    MapName = MapInfoMapper.GetMapName(currentRecord.Id),  // ✅ 即時獲取最新名稱
                    RecordId = currentRecord.RecordId,
                    MapTicket = currentRecord.MapTicket,
                    Compass = currentRecord.Compass.Where(c => !string.IsNullOrEmpty(c)).ToArray(),
                    Probe = currentRecord.Probe,
                    StartTime = currentRecord.StartTime,
                    Items = currentRecord.PickRecord?.Select(p => new
                    {
                        p.Value.BaseId,
                        p.Value.Name,
                        p.Value.Total,
                        Slots = p.Value.Slots
                    }).OrderByDescending(i => i.Total).ToArray() ?? Array.Empty<object>()
                };
            }
            else
            {
                // 在異界地圖但沒有記錄
                return new
                {
                    IsInMap = true,
                    MapType = "Netherrealm",
                    MapName = _mapPickRecordManager.CurrentMapName,
                    RecordId = (Guid?)null,
                    MapTicket = "",
                    Compass = Array.Empty<string>(),
                    Probe = "",
                    StartTime = (DateTime?)null,
                    Items = Array.Empty<object>()
                };
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
