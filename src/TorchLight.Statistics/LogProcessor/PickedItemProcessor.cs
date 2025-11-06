using Serilog;

namespace TorchLight.Statistics.LogProcessor;

/// <summary>
/// 拾取物品處理器 - 繼承自 BaseLogProcessor
/// </summary>
public class PickedItemProcessor : BaseLogProcessor
{
    public event Action<ItemChangeEvent> OnItemsPicked;

    protected override bool IsBlockStart(string line)
    {
        return line.Contains("ItemChange@ ProtoName=PickItems start") || 
               line.Contains("ItemChange@ ProtoName=PickItem start");
    }

    protected override bool IsBlockEnd(string line)
    {
        return line.Contains("ItemChange@ ProtoName=PickItems end") || 
               line.Contains("ItemChange@ ProtoName=PickItem end");
    }

    protected override void OnBlockStart(string line)
    {
        Log.Debug("開始拾取物品區塊");
    }

    protected override void OnBlockEnd(string line)
    {
        Log.Debug("結束拾取物品區塊");
    }

    protected override void ProcessBlockLine(string line)
    {
        if (LineParser.IsBagMgr(line, "BagMgr@:Modfy BagItem", out var itemChangeEvent))
        {
            itemChangeEvent.ProtoName = "PickItems";
            OnItemsPicked?.Invoke(itemChangeEvent);
        }
    }
}
