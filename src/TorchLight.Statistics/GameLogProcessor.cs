using TorchLight.Statistics.Services;

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

    public GameLogProcessor(
           Dictionary<int, string> itemIdTable,
           LineParser lineParser,
           ItemChangeBlockProcessor itemChangeProcessor)
    {
        _lineParser = lineParser ?? throw new ArgumentNullException(nameof(lineParser));
        _itemChangeProcessor = itemChangeProcessor ?? throw new ArgumentNullException(nameof(itemChangeProcessor));
        _bagInventoryManager = new BagInventoryManager(itemIdTable);
        _mapPickRecordManager = new MapPickRecordManager(itemIdTable);
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
            // 1. 初始化背包物品
            if (_lineParser.IsInitBagItemData(line))
            {
                var itemData = _lineParser.GetItemData(line);
                _bagInventoryManager.InitializeBagItem(itemData);
                return;
            }

            // 2. 初始化完成
            if (_lineParser.IsInitFinished(line))
            {
                _bagInventoryManager.PrintInitializedBag();
                return;
            }

            // 3. 登入開始 - 重置所有資料
            if (_lineParser.IsLoginStart(line))
            {
                Console.WriteLine("\n=== 偵測到重新登入，重置所有資料 ===\n");
                _bagInventoryManager.Reset();
                _mapPickRecordManager.Reset();
                return;
            }

            // 4. 地圖切換
            if (_lineParser.IsMoveMap(line))
            {
                var (time, fromPath, toPath, success) = _lineParser.GetMapPathData(line);
                if (success)
                {
                    _mapTransitionHandler.HandleMapTransition(time, fromPath, toPath);
                }
                return;
            }

            // 5. 處理物品變更（區塊處理）
            _itemChangeProcessor.HandleLine(line);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[錯誤] 處理日誌行時發生錯誤: {ex.Message}");
            Console.WriteLine($"[錯誤] 日誌內容: {line}");
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

            // 如果是增加物品（拾取），且在異界地圖中，則記錄拾取
            if (bagResult.QuantityChange > 0 && ev.ProtoName == "PickItems")
            {
                var mapResult = _mapPickRecordManager.RecordPickedItem(
               ev.ConfigBaseId,
                    ev.SlotId,
                  bagResult.QuantityChange);

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
