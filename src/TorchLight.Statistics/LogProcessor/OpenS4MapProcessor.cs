using Serilog;

namespace TorchLight.Statistics.LogProcessor;

public class OpenS4MapProcessor: BaseLogProcessor
{
    public event Action<DateTime> OnMapStart;
    public event Action<OpenMapEvent> OnMapComplete;
    public event Action<ItemChangeEvent> OnItemChangeInMapBlock;

    private OpenMapEvent _currentMapEvent = null;

    private const string S4MapStartIndicator = "PageApplyBase@ EnterScene ScenePath = World'/Game/Art/Maps/S4";

    protected override bool IsBlockStart(string line)
    {
        return LineParser.GetLineDateTime(line, S4MapStartIndicator, out _);
    }

    protected override bool IsBlockEnd(string line)
    {
        return line.Contains("[Game] UGameMgr::EnterLevel");
    }

    protected override void OnBlockStart(string line)
    {
        if (LineParser.GetLineDateTime(line, S4MapStartIndicator, out var startTime))
        {
            Log.Debug("開始開S4賽季地圖");
            _currentMapEvent = new OpenMapEvent(startTime);
            OnMapStart?.Invoke(startTime);
        }
    }

    protected override void OnBlockEnd(string line)
    {
        Log.Debug("S4賽季地圖開啟完成");
        OnMapComplete?.Invoke(_currentMapEvent);
        _currentMapEvent = null;
    }

    protected override void ProcessBlockLine(string line)
    {
        if (_currentMapEvent == null)
            return;

        // 解析地圖 Token
        if (LineParser.IsTokenLine(line, "+AreaUniqueId [", out string token))
        {
            _currentMapEvent.Token = token;
            Log.Debug("賽季地圖 Token: {Token}", token);
        }
        // 解析地圖 ID
        else if (LineParser.IsCurrentOpenMapIDLine(line, "+mapId [", out int mapId))
        {
            _currentMapEvent.MapId = mapId;
            Log.Debug("賽季地圖 ID: {MapId}", mapId);            
        }
        else if(LineParser.GetCellValue(line, "+maptype [", out string mapType))
        {
            _currentMapEvent.MapType = mapType;
            Log.Debug("S4 賽季地圖Type: {type}", mapType);
        }
    }

    public override void Reset()
    {
        base.Reset();
        _currentMapEvent = null;
    }
}
