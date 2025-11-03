using Serilog;

namespace TorchLight.Statistics.LogProcessor;

/// <summary>
/// 物品變更事件（簡化版，單線程）
/// </summary>
public class ItemChangeEvent
{
    public DateTime Time { get; set; }
    public int PageId { get; set; }
    public int SlotId { get; set; }
    public int ConfigBaseId { get; set; }
    public int Num { get; set; }
    public string ProtoName { get; set; }
    public string Action { get; set; }
}

/// <summary>
/// 物品變更處理器（簡化版，單線程）
/// 負責處理 ItemChange 區塊內的背包修改事件
/// </summary>
public class ItemChangeProcessor
{
    /// <summary>
    /// 目標協議名稱（只處理這些協議的區塊）
    /// </summary>
    private readonly HashSet<string> _targetProtocols = ["Spv3Open", "PickItem", "PickItems", "Push2"];
    // Spv3Open: 開啟地圖扣除地圖耗材(迴響，羅盤探針)
    // PickItem/PickItems: 拾取物品
    // Push2: 從拍賣場收火
    // XchgForSale: 拍賣場上架物品（未加入）
    // ResetItemsLayout: 整理包包排序（未加入）

    #region 事件

    /// <summary>
    /// 當區塊開始時觸發
    /// </summary>
    public event Action<string, DateTime> OnBlockStarted;

    /// <summary>
    /// 當區塊內發生背包修改時立即觸發
    /// </summary>
    public event Action<ItemChangeEvent> OnItemChanged;

    /// <summary>
    /// 當區塊結束時觸發
    /// </summary>
    public event Action<string, DateTime, List<ItemChangeEvent>> OnBlockEnded;

    #endregion

    #region 私有欄位

    private bool _inBlock = false;
    private string _currentProtoName = string.Empty;
    private DateTime _blockStartTime;
    private readonly List<ItemChangeEvent> _eventBuffer = [];

    #endregion

    /// <summary>
    /// 處理單行日誌
    /// </summary>
    public void HandleLine(string line)
    {
        // 1) 檢查區塊開始
        if (TryParseBlockStart(line, out var blockStarted))
        {
            HandleBlockStart(blockStarted);
            return;
        }

        // 2) 檢查區塊結束
        if (TryParseBlockEnd(line, out var blockEnded))
        {
            HandleBlockEnd(blockEnded);
            return;
        }

        // 3) 檢查背包修改（只在區塊內處理）
        if (_inBlock && TryParseBagModification(line, out var itemChangeEvent))
        {
            HandleItemChange(itemChangeEvent);
            return;
        }

        // 4) 檢查背包刪除（只在區塊內處理）
        if (_inBlock && TryParseBagDeletion(line, out var deleteEvent))
        {
            HandleItemChange(deleteEvent);
        }
    }

    #region 解析方法

    private bool TryParseBlockStart(string line, out BlockStarted blockStarted)
    {
        blockStarted = null;
        var match = LineRegex.StartLine().Match(line);
        if (!match.Success) return false;

        var time = LineParser.ParseUnrealDateTime(match.Groups["time"].Value);
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

        var time = LineParser.ParseUnrealDateTime(match.Groups["time"].Value);
        var threadId = int.Parse(match.Groups["tid"].Value);
        var protoName = match.Groups["proto"].Value;

        blockEnded = new BlockEnded(time, threadId, protoName);
        return true;
    }

    private bool TryParseBagModification(string line, out ItemChangeEvent itemEvent)
    {
        itemEvent = null;
        var match = LineRegex.BagItemLine().Match(line);
        if (!match.Success) return false;

        var time = LineParser.ParseUnrealDateTime(match.Groups["time"].Value);

        itemEvent = new ItemChangeEvent
        {
            Time = time,
            PageId = int.Parse(match.Groups["page"].Value),
            SlotId = int.Parse(match.Groups["slot"].Value),
            ConfigBaseId = int.Parse(match.Groups["config"].Success ? match.Groups["config"].Value : "0"),
            Num = int.Parse(match.Groups["num"].Success ? match.Groups["num"].Value : "0"),
            ProtoName = _currentProtoName,
            Action = match.Groups["action"].Value
        };

        return true;
    }

    private bool TryParseBagDeletion(string line, out ItemChangeEvent deleteEvent)
    {
        deleteEvent = null;
        var match = LineRegex.BagItemDeleteLine().Match(line);
        if (!match.Success) return false;

        var time = LineParser.ParseUnrealDateTime(match.Groups["time"].Value);

        deleteEvent = new ItemChangeEvent
        {
            Time = time,
            PageId = int.Parse(match.Groups["page"].Value),
            SlotId = int.Parse(match.Groups["slot"].Value),
            ConfigBaseId = int.Parse(match.Groups["config"].Value),
            Num = 0,
            ProtoName = _currentProtoName,
            Action = "RemoveBagItem"
        };

        return true;
    }

    #endregion

    #region 事件處理

    private void HandleBlockStart(BlockStarted startEvent)
    {
        _inBlock = true;
        _currentProtoName = startEvent.ProtoName;
        _blockStartTime = startEvent.Time;
        _eventBuffer.Clear();

        Log.Debug("物品變更區塊開始: {ProtoName}", _currentProtoName);
        OnBlockStarted?.Invoke(_currentProtoName, _blockStartTime);
    }

    private void HandleBlockEnd(BlockEnded endEvent)
    {
        if (!_inBlock)
            return;

        // 只處理目標協議的結束
        if (!_targetProtocols.Contains(endEvent.ProtoName))
            return;

        Log.Debug("物品變更區塊結束: {ProtoName}, 共 {Count} 個事件", endEvent.ProtoName, _eventBuffer.Count);

        _inBlock = false;
        OnBlockEnded?.Invoke(endEvent.ProtoName, endEvent.Time, [.. _eventBuffer]);
        _eventBuffer.Clear();
    }

    private void HandleItemChange(ItemChangeEvent itemEvent)
    {
        // 即時模式：立即通知
        OnItemChanged?.Invoke(itemEvent);

        // 彙整模式：緩存到區塊
        _eventBuffer.Add(itemEvent);

        Log.Debug("物品變更: {Action} PageId={PageId} SlotId={SlotId} ConfigBaseId={ConfigBaseId} Num={Num}",
            itemEvent.Action, itemEvent.PageId, itemEvent.SlotId, itemEvent.ConfigBaseId, itemEvent.Num);
    }

    #endregion

    #region 公開方法

    /// <summary>
    /// 取得當前是否在區塊內
    /// </summary>
    public bool IsInBlock => _inBlock;

    /// <summary>
    /// 取得當前協議名稱
    /// </summary>
    public string CurrentProtoName => _currentProtoName;

    /// <summary>
    /// 取得當前緩存的事件數量
    /// </summary>
    public int BufferedEventCount => _eventBuffer.Count;

    /// <summary>
    /// 手動重置狀態（用於異常情況）
    /// </summary>
    public void Reset()
    {
        _inBlock = false;
        _currentProtoName = string.Empty;
        _blockStartTime = DateTime.MinValue;
        _eventBuffer.Clear();
        Log.Warning("物品變更處理器已重置");
    }

    #endregion
}
