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
/// 背包初始化處理器 - 繼承自 BaseLogProcessor
/// </summary>
public class InitBagProcessor : BaseLogProcessor
{   
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

    private InitBagEvent _currentInitEvent = null;

    protected override bool IsBlockStart(string line)
    {
        var (isInitLine, shouldProcess, _, isFirstInit) = LineParser.CheckBagInitializationState(line);
        var result = isInitLine && shouldProcess && isFirstInit;
        if (result)
        {
            // 初始化背包的第一行也要計算            
            _currentInitEvent = new InitBagEvent
            {
                StartTime = DateTime.Now
            };
            TryParseInitItem(line);
        }
        return result;
    }

    protected override bool IsBlockEnd(string line)
    {
        var (_, _, isComplete, _) = LineParser.CheckBagInitializationState(line);
        return isComplete;
    }

    protected override void OnBlockStart(string line)
    {
        Log.Information("背包初始化開始");
        OnInitStarted?.Invoke(_currentInitEvent.StartTime);
    }

    protected override void OnBlockEnd(string line)
    {
        if (_currentInitEvent == null)
            return;

        _currentInitEvent.CompleteTime = DateTime.Now;

        Log.Information("背包初始化完成，共 {Count} 種物品", _currentInitEvent.Items.Count);
        OnInitCompleted?.Invoke(_currentInitEvent);

        _currentInitEvent = null;
    }

    protected override void ProcessBlockLine(string line)
    {
        var (isInitLine, shouldProcess, _, _) = LineParser.CheckBagInitializationState(line);
        
        if (isInitLine && shouldProcess)
        {
            TryParseInitItem(line);
        }
    }

    private void TryParseInitItem(string line)
    {
        if (TryParseInitItem(line, out var item))
        {
            // 即時模式：立即通知
            OnItemInitialized?.Invoke(item);

            // 彙整模式：緩存到事件
            _currentInitEvent?.Items.Add(item);
        }
    }
    /// <summary>
    /// 解析初始化物品
    /// </summary>
    private static bool TryParseInitItem(string line, out ItemModel item)
    {
        try
        {
            item = LineParser.GetItemData(line);
            return item != null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "解析初始化物品失敗");
            item = null;
            return false;
        }
    }

    /// <summary>
    /// 取得當前已初始化的物品數量
    /// </summary>
    public int InitializedItemCount => _currentInitEvent?.Items.Count ?? 0;

    /// <summary>
    /// 重置初始化狀態（登入時使用）
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        _currentInitEvent = null;
        LineParser.ResetInitializationState();
        Log.Information("背包初始化處理器已重置");
    }
}
