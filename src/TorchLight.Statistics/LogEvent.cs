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