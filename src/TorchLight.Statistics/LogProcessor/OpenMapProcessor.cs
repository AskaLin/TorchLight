using Serilog;

namespace TorchLight.Statistics.LogProcessor;

/// <summary>
/// 開啟地圖事件
/// </summary>
public class OpenMapEvent(DateTime startTime)
{
    public string Token { get; set; }
    public int MapId { get; set; }
    public int LevelId { get; set; }
    public DateTime StartTime { get; set; } = startTime;
}

/// <summary>
/// 開啟地圖處理器 - 繼承自 BaseLogProcessor
/// </summary>
public class OpenMapProcessor : BaseLogProcessor
{
    public event Action<DateTime> OnMapStart;
    public event Action<OpenMapEvent> OnMapComplete;
    public event Action<ItemChangeEvent> OnItemChangeInMapBlock;

    private OpenMapEvent _currentMapEvent = null;

    protected override bool IsBlockStart(string line)
    {
        return LineParser.GetLineDateTime(line, "ItemChange@ ProtoName=Spv3Open start", out _);
    }

    protected override bool IsBlockEnd(string line)
    {
        return line.Contains($"[Game] UGameMgr::EnterLevel({_currentMapEvent.LevelId})");
    }

    protected override void OnBlockStart(string line)
    {
        if (LineParser.GetLineDateTime(line, "ItemChange@ ProtoName=Spv3Open start", out var startTime))
        {
            Log.Debug("開始開新圖");
            _currentMapEvent = new OpenMapEvent(startTime);
            OnMapStart?.Invoke(startTime);
        }
    }

    protected override void OnBlockEnd(string line)
    {
        Log.Debug("地圖開啟完成, 並進入");
        OnMapComplete?.Invoke(_currentMapEvent);
        _currentMapEvent = null;
    }

    protected override void ProcessBlockLine(string line)
    {
        if (_currentMapEvent == null)
            return;

        // 解析地圖 Token
        if (LineParser.GetCellValue(line, "+AreaUniqueId [", out string token))
        {
            _currentMapEvent.Token = token;
            Log.Debug("地圖 Token: {Token}", token);
        }
        // 解析地圖 levelId
        else if (LineParser.GetCellValue(line, "+levelId [", out int levelId))
        {
            _currentMapEvent.LevelId = levelId;
            Log.Debug("地圖 等級: {Level}", levelId);
        }
        // 解析地圖 ID
        else if (LineParser.GetCellValue(line, "+mapId [", out int mapId))
        {
            _currentMapEvent.MapId = mapId;
            Log.Debug("地圖 ID: {MapId}", mapId);
        }
        // 處理開圖材料消耗
        else if (LineParser.IsBagMgr(line, "BagMgr@:Modfy BagItem", out var itemChangeEvent))
        {
            itemChangeEvent.ProtoName = "Spv3Open";
            OnItemChangeInMapBlock?.Invoke(itemChangeEvent);
        }
    }

    public override void Reset()
    {
        base.Reset();
        _currentMapEvent = null;
    }
}
