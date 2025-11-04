namespace TorchLight.Statistics.LogProcessor;

public class PickedItemProcessor
{
    public event Action<ItemChangeEvent> OnItemsPicked;

    private bool _inPickItemBlock = false;

    public bool HandleLine(string line)
    {
        if (line.Contains("ItemChange@ ProtoName=PickItems start") || line.Contains("ItemChange@ ProtoName=PickItem start"))
        {
            _inPickItemBlock = true;
            return true;
        }
        else if (_inPickItemBlock)
        {
            if (LineParser.IsBagMgr(line, "BagMgr@:Modfy BagItem", out var itemChangeEvent))
            {
                itemChangeEvent.ProtoName = "PickItems";
                OnItemsPicked?.Invoke(itemChangeEvent);
            }
            else if (line.Contains("ItemChange@ ProtoName=PickItems end") || line.Contains("ItemChange@ ProtoName=PickItem end"))
            {
                _inPickItemBlock = false;
            }
            return true;
        }
        return false;
    }
}
