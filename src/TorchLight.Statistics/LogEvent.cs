using TorchLight.Statistics.Configuration;

namespace TorchLight.Statistics;

/// <summary>
/// 日誌行事件基類
/// </summary>
public abstract record LogEvent(DateTime Time, int ThreadId);

/// <summary>
/// 背包修改事件
/// </summary>
public record BagModEvent(
    DateTime Time,
    int ThreadId,
    int PageId,
    int SlotId,
    int ConfigBaseId,
    int Num,
    string ProtoName,
    string Action
) : LogEvent(Time, ThreadId);

/// <summary>
/// 區塊開始事件
/// </summary>
public record BlockStarted(DateTime Time, int ThreadId, string ProtoName) : LogEvent(Time, ThreadId);

/// <summary>
/// 區塊結束事件
/// </summary>
public record BlockEnded(DateTime Time, int ThreadId, string ProtoName) : LogEvent(Time, ThreadId);

/// <summary>
/// 物品變更區塊上下文（用於追蹤每個執行緒的區塊狀態）
/// </summary>
public sealed class ItemChangeBlockContext
{
    public bool InBlock { get; set; }
    public string ProtoName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public List<BagModEvent> Buffer { get; } = [];

    public void Reset()
    {
        InBlock = false;
        ProtoName = string.Empty;
        StartTime = DateTime.MinValue;
        Buffer.Clear();
    }
}

/// <summary>
/// 物品變更區塊處理器
/// 負責識別和處理 ItemChange 區塊（start/end）內的背包修改事件
/// </summary>
public sealed class ItemChangeBlockProcessor
{
    /// <summary>
    /// 依 ThreadId 記錄各自的區塊狀態
    /// </summary>
    private readonly Dictionary<int, ItemChangeBlockContext> _contexts = [];

    /// <summary>
    /// 目標協議名稱（只處理這些協議的區塊）
    /// </summary>
    private readonly HashSet<string> _targetProtocols = ["Spv3Open", "PickItems"];
    // 暫時理解 Spv3Open 是開啟地圖扣除地圖耗材(迴響，羅盤探針)
    // 拍賣場: XchgForSale
    // 整理包包排序: ResetItemsLayout


    #region 事件

    /// <summary>
    /// 當偵測到區塊開始時觸發
    /// </summary>
    public event Action<BlockStarted> OnBlockStarted;

    /// <summary>
    /// 當區塊內發生背包修改時立即觸發（即時模式）
    /// </summary>
    public event Action<BagModEvent> OnBagModInsideBlock;

    /// <summary>
    /// 當區塊結束時觸發，並提供該區塊內所有的背包修改事件（彙整模式）
    /// </summary>
    public event Action<BlockEnded, IReadOnlyList<BagModEvent>> OnBlockEndedWithBatch;

    #endregion

    /// <summary>
    /// 處理單行日誌
    /// </summary>
    public void HandleLine(string line)
    {
        // 1) 檢查區塊開始
        if (TryParseBlockStart(line, out var startEvent))
        {
            HandleBlockStart(startEvent);
            return;
        }

        // 2) 檢查區塊結束
        if (TryParseBlockEnd(line, out var endEvent))
        {
            HandleBlockEnd(endEvent);
            return;
        }

        // 3) 檢查背包修改
        if (TryParseBagModification(line, out var bagEvent))
        {
            HandleBagModification(bagEvent);
            return;
        }

        // 4) 檢查背包刪除
        if (TryParseBagDeletion(line, out var deleteEvent))
        {
            HandleBagModification(deleteEvent);
        }
    }

    #region 解析方法

    private bool TryParseBlockStart(string line, out BlockStarted blockStarted)
    {
        blockStarted = null;
        var match = LineRegex.StartLine().Match(line);
        if (!match.Success) return false;

        var time = ParseDateTime(match.Groups["time"].Value);
        var threadId = int.Parse(match.Groups["tid"].Value);
        var protoName = match.Groups["proto"].Value;

        if (!_targetProtocols.Contains(protoName))
            return false;

        blockStarted = new BlockStarted(time, threadId, protoName);
        return true;
    }

    private static bool TryParseBlockEnd(string line, out BlockEnded blockEnded)
    {
        blockEnded = null;
        var match = LineRegex.EndLine().Match(line);
        if (!match.Success) return false;

        var time = ParseDateTime(match.Groups["time"].Value);
        var threadId = int.Parse(match.Groups["tid"].Value);
        var protoName = match.Groups["proto"].Value;

        blockEnded = new BlockEnded(time, threadId, protoName);
        return true;
    }

    private bool TryParseBagModification(string line, out BagModEvent bagEvent)
    {
        bagEvent = null;
        var match = LineRegex.BagItemLine().Match(line);
        if (!match.Success) return false;

        var time = ParseDateTime(match.Groups["time"].Value);
        var threadId = int.Parse(match.Groups["tid"].Value);

        var context = GetContext(threadId);
        if (!context.InBlock || !_targetProtocols.Contains(context.ProtoName))
            return false;

        bagEvent = new BagModEvent(
            time,
            threadId,
            PageId: int.Parse(match.Groups["page"].Value),
            SlotId: int.Parse(match.Groups["slot"].Value),
            ConfigBaseId: int.Parse(match.Groups["config"].Success ? match.Groups["config"].Value : "0"),
            Num: int.Parse(match.Groups["num"].Success ? match.Groups["num"].Value : "0"),
            ProtoName: context.ProtoName,
            Action: match.Groups["action"].Value
        );

        return true;
    }

    private bool TryParseBagDeletion(string line, out BagModEvent deleteEvent)
    {
        deleteEvent = null;
        var match = LineRegex.BagItemDeleteLine().Match(line);
        if (!match.Success) return false;

        var time = ParseDateTime(match.Groups["time"].Value);
        var threadId = int.Parse(match.Groups["tid"].Value);

        var context = GetContext(threadId);
        if (!context.InBlock || !_targetProtocols.Contains(context.ProtoName))
            return false;

        deleteEvent = new BagModEvent(
            time,
            threadId,
            PageId: int.Parse(match.Groups["page"].Value),
            SlotId: int.Parse(match.Groups["slot"].Value),
            ConfigBaseId: int.Parse(match.Groups["config"].Value),
            Num: 0,
            ProtoName: context.ProtoName,
            Action: "RemoveBagItem"
        );

        return true;
    }

    #endregion

    #region 事件處理

    private void HandleBlockStart(BlockStarted startEvent)
    {
        var context = GetOrCreateContext(startEvent.ThreadId);
        context.InBlock = true;
        context.ProtoName = startEvent.ProtoName;
        context.StartTime = startEvent.Time;
        context.Buffer.Clear();

        OnBlockStarted?.Invoke(startEvent);
    }

    private void HandleBlockEnd(BlockEnded endEvent)
    {
        if (!_contexts.TryGetValue(endEvent.ThreadId, out var context))
            return;

        if (!context.InBlock || !_targetProtocols.Contains(context.ProtoName))
            return;

        context.InBlock = false;
        OnBlockEndedWithBatch?.Invoke(endEvent, context.Buffer.AsReadOnly());
        context.Buffer.Clear();
    }

    private void HandleBagModification(BagModEvent bagEvent)
    {
        // 即時模式：立即通知
        OnBagModInsideBlock?.Invoke(bagEvent);

        // 彙整模式：緩存到區塊
        var context = GetContext(bagEvent.ThreadId);
        context.Buffer.Add(bagEvent);
    }

    #endregion

    #region 輔助方法

    private ItemChangeBlockContext GetContext(int threadId)
    {
        return _contexts.TryGetValue(threadId, out var ctx) ? ctx : new ItemChangeBlockContext();
    }

    private ItemChangeBlockContext GetOrCreateContext(int threadId)
    {
        if (!_contexts.TryGetValue(threadId, out var context))
        {
            context = new ItemChangeBlockContext();
            _contexts[threadId] = context;
        }
        return context;
    }

    private static DateTime ParseDateTime(string timeStr)
    {
        return DateTime.ParseExact(timeStr, AppConfiguration.UnrealLogTimeFormat, null)
            .AddHours(AppConfiguration.TimeZoneOffsetHours);
    }

    #endregion

    /// <summary>
    /// 保護性機制：自動關閉超時的區塊，避免因漏掉 end 而導致狀態異常
    /// </summary>
    public void CloseStaleBlocks(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        foreach (var (threadId, context) in _contexts)
        {
            if (context.InBlock && (now - context.StartTime.ToUniversalTime()) > timeout)
            {
                context.InBlock = false;
                var endEvent = new BlockEnded(DateTime.Now, threadId, context.ProtoName);
                OnBlockEndedWithBatch?.Invoke(endEvent, context.Buffer.AsReadOnly());
                context.Buffer.Clear();
            }
        }
    }
}
