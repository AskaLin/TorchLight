using Serilog;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.LogProcessor;

/// <summary>
/// 背包初始化事件
/// </summary>
public class InitBagEvent
{
    public DateTime StartTime { get; set; }
    public DateTime CompleteTime { get; set; }
    public List<ItemModel> Items { get; } = [];
}

/// <summary>
/// 背包初始化處理器（簡化版，單線程）
/// 負責處理背包初始化區塊內的物品資料
/// </summary>
public class InitBagProcessor
{
    private readonly LineParser _lineParser;

    public InitBagProcessor(LineParser lineParser)
    {
        _lineParser = lineParser ?? throw new ArgumentNullException(nameof(lineParser));
    }

    #region 事件

    /// <summary>
    /// 當背包初始化開始時觸發
    /// </summary>
    public event Action<DateTime> OnInitStarted;

    /// <summary>
    /// 當背包物品初始化時立即觸發（即時模式）
    /// </summary>
    public event Action<ItemModel> OnItemInitialized;

    /// <summary>
    /// 當背包初始化完成時觸發（彙整模式）
    /// </summary>
    public event Action<InitBagEvent> OnInitCompleted;

    #endregion

    #region 私有欄位

    private InitBagEvent _currentInitEvent = null;

    #endregion

    /// <summary>
    /// 處理單行日誌
    /// </summary>
    public void HandleLine(string line)
    {
        // 檢查初始化狀態
        var (isInitLine, shouldProcess, isComplete, isFirstInit) = _lineParser.CheckBagInitializationState(line);

        // 1) 初始化開始（第一次遇到初始化行）
        if (isInitLine && shouldProcess && isFirstInit)
        {
            HandleInitStart();
        }

        // 2) 處理初始化物品
        if (isInitLine && shouldProcess)
        {
            if (TryParseInitItem(line, out var item))
            {
                HandleItemInit(item);
            }
        }

        // 3) 初始化完成
        if (isComplete)
        {
            HandleInitComplete();
        }
    }

    #region 解析方法

    /// <summary>
    /// 解析初始化物品
    /// </summary>
    private bool TryParseInitItem(string line, out ItemModel item)
    {
        try
        {
            // 使用 LineParser.GetItemData 解析
            item = _lineParser.GetItemData(line);
            return item != null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "解析初始化物品失敗");
            item = null;
            return false;
        }
    }

    #endregion

    #region 事件處理

    private void HandleInitStart()
    {
        var startTime = DateTime.Now;
        _currentInitEvent = new InitBagEvent
        {
            StartTime = startTime
        };

        Log.Information("背包初始化開始");
        OnInitStarted?.Invoke(startTime);
    }

    private void HandleItemInit(ItemModel item)
    {
        // 即時模式：立即通知
        OnItemInitialized?.Invoke(item);

        // 彙整模式：緩存到事件
        _currentInitEvent?.Items.Add(item);

        // Log.Debug("初始化背包物品: {ItemName} x{Count}", item.Name, item.Num);
    }

    private void HandleInitComplete()
    {
        if (_currentInitEvent == null)
            return;

        _currentInitEvent.CompleteTime = DateTime.Now;

        Log.Information("背包初始化完成，共 {Count} 種物品", _currentInitEvent.Items.Count);
        OnInitCompleted?.Invoke(_currentInitEvent);

        _currentInitEvent = null;
    }

    #endregion

    #region 公開方法

    /// <summary>
    /// 取得當前已初始化的物品數量
    /// </summary>
    public int InitializedItemCount => _currentInitEvent?.Items.Count ?? 0;

    /// <summary>
    /// 重置初始化狀態（登入時使用）
    /// </summary>
    public void Reset()
    {
        _currentInitEvent = null;
        _lineParser.ResetInitializationState();
        Log.Information("背包初始化處理器已重置");
    }

    #endregion
}
