using Serilog;

namespace TorchLight.Statistics.LogProcessor;

/// <summary>
/// 推送物品處理器 - 繼承自 BaseLogProcessor
/// </summary>
public class PushItemProcessor : BaseLogProcessor
{
    public event Action<ItemChangeEvent> OnItemsPushed;

    protected override bool IsBlockStart(string line)
    {
        return line.Contains("ItemChange@ ProtoName=Push2 start");
    }

    protected override bool IsBlockEnd(string line)
    {
        return line.Contains("ItemChange@ ProtoName=Push2 end");
    }

    protected override void OnBlockStart(string line)
    {
        Log.Debug("開始推送物品區塊");
    }

    protected override void OnBlockEnd(string line)
    {
        Log.Debug("結束推送物品區塊");
    }

    protected override void ProcessBlockLine(string line)
    {
        if (LineParser.IsBagMgr(line, "BagMgr@:", out var itemChangeEvent))
        {
            itemChangeEvent.ProtoName = "Push2";
            OnItemsPushed?.Invoke(itemChangeEvent);
        }
    }
}
