using Serilog;
using TorchLight.Statistics.Services;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics;

/// <summary>
/// 遊戲日誌處理器 - 統籌所有日誌處理邏輯
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

    public GameLogProcessor(
           Dictionary<int, ItemModel> itemTable,
           LineParser lineParser,
           ItemChangeBlockProcessor itemChangeProcessor)
    {
        _lineParser = lineParser ?? throw new ArgumentNullException(nameof(lineParser));
        _itemChangeProcessor = itemChangeProcessor ?? throw new ArgumentNullException(nameof(itemChangeProcessor));
        _itemTable = itemTable ?? throw new ArgumentNullException(nameof(itemTable));
        _bagInventoryManager = new BagInventoryManager(itemTable);
        _mapPickRecordManager = new MapPickRecordManager(itemTable);
        _mapTransitionHandler = new MapTransitionHandler(_mapPickRecordManager);
        _logger = new ConsoleLogger();

        // 註冊事件處理
        _itemChangeProcessor.OnBagModInsideBlock += HandleBagModification;
    }

    /// <summary>
    /// 處理單行日誌
    /// </summary>
    public void ProcessLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        try
        {
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
            // 處理 Spv3Open 事件（開圖材料）
            if (ev.ProtoName == "Spv3Open" && _itemTable.TryGetValue(ev.ConfigBaseId, out var item))
            {
                // 記錄羅盤、探針和門票作為開圖材料
                if (item.Type == ItemType.Compass || item.Type == ItemType.Probe || 
                    item.Type == ItemType.MapTicket || item.Type == ItemType.BossTicket || 
                    item.Type == ItemType.GameplayTicket)
                {
                    _mapPickRecordManager.RecordMapMaterial(ev.ConfigBaseId, item.Type);
                }
            }

            // 更新背包庫存
            var bagResult = _bagInventoryManager.UpdateBagItem(ev);

            // 記錄日誌
            _logger.LogBagModification(ev, bagResult);

            // 如果是增加物品（拾取），且在異界地圖中，則記錄拾取
            if (bagResult.QuantityChange > 0 && ev.ProtoName == "PickItems")
            {
                var mapResult = _mapPickRecordManager.RecordPickedItem(ev.ConfigBaseId, ev.SlotId, bagResult.QuantityChange);
                if (mapResult != null)
                {
                    _logger.LogMapPickItem(_mapPickRecordManager.CurrentMapName, mapResult);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[錯誤] 處理背包修改時發生錯誤: {ex.Message}");
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
