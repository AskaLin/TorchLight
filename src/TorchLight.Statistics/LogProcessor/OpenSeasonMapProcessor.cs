using Serilog;

namespace TorchLight.Statistics.LogProcessor;

/// <summary>
/// 開啟賽季地圖處理器 - 繼承自 BaseLogProcessor
/// </summary>
public class OpenSeasonMapProcessor : BaseLogProcessor
{
    public event Action<DateTime> OnMapStart;
    public event Action<OpenMapEvent> OnMapComplete;
    public event Action<ItemChangeEvent> OnItemChangeInMapBlock;

    private OpenMapEvent _currentMapEvent = null;

    protected override bool IsBlockStart(string line)
    {
        return LineParser.GetLineDateTime(line, "PageApplyBase@ EnterScene ScenePath = World'/Game/Art/Season/", out _);
    }

    protected override bool IsBlockEnd(string line)
    {
        return line.Contains("[Game] UGameMgr::EnterLevel");
    }

    protected override void OnBlockStart(string line)
    {
        if (LineParser.GetLineDateTime(line, "PageApplyBase@ EnterScene ScenePath = World'/Game/Art/Season/", out var startTime))
        {
            Log.Debug("開始開賽季地圖");
            _currentMapEvent = new OpenMapEvent(startTime);
            OnMapStart?.Invoke(startTime);
        }
    }

    protected override void OnBlockEnd(string line)
    {
        Log.Debug("賽季地圖開啟完成");
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
    }

    public override void Reset()
    {
        base.Reset();
        _currentMapEvent = null;
    }
}
