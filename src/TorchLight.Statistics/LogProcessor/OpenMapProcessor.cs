using Serilog;

namespace TorchLight.Statistics.LogProcessor;

public class OpenMapEvent(DateTime startTime)
{
    public string Token { get; set; }
    public int MapId { get; set; }
    public int Level { get; set; }
    public DateTime StartTime { get; set; } = startTime;
}

public class OpenMapProcessor
{
    public event Action<DateTime> OnMapStart;
    public event Action<OpenMapEvent> OnMapComplete;
    public event Action<ItemChangeEvent> OnItemChangeInMapBlock;

    private bool _inOpenMapBlock = false;
    private OpenMapEvent currentMapEvent = null;

    public bool HandleLine(string line)
    {
        if (LineParser.GetLineDateTime(line, "ItemChange@ ProtoName=Spv3Open start", out var startTime))
        {
            Log.Debug("開始開新圖");
            _inOpenMapBlock = true;
            currentMapEvent = new OpenMapEvent(startTime);
            OnMapStart?.Invoke(startTime);
            return true;
        }
        else if (_inOpenMapBlock)
        {
            if (LineParser.IsTokenLine(line, "+AreaUniqueId [", out string token))
            {
                currentMapEvent.Token = token;
                Log.Debug($"地圖 Token: {token}");
            }
            //else if (LineParser.IsCurrentLevelLine(line, out int level))
            //{
            //    currentMapEvent.Level = level;
            //    Log.Debug($"地圖 Level: {level}");
            //}
            else if (LineParser.IsCurrentOpenMapIDLine(line, "+mapId [", out int mapId))
            {
                currentMapEvent.MapId = mapId;
                Log.Debug($"地圖 ID: {mapId}");
            }
            else if (LineParser.IsBagMgr(line, "BagMgr@:Modfy BagItem", out var itemChangeEvent))
            {
                itemChangeEvent.ProtoName = "Spv3Open";
                OnItemChangeInMapBlock?.Invoke(itemChangeEvent);
            }
            else if (line.Contains("[Game] UGameMgr::EnterLevel"))
            {
                _inOpenMapBlock = false;
                OnMapComplete?.Invoke(currentMapEvent);
            }
            return true;
        }
        return false;
    }
}
