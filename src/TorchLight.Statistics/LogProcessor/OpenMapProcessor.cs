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

    private bool inOpenMapBlock = false;
    private OpenMapEvent currentMapEvent = null;

    public void HandleLine(string line)
    {
        if (LineParser.OpenMapStart(line, out var startTime))
        {
            inOpenMapBlock = true;
            currentMapEvent = new OpenMapEvent(startTime);
            OnMapStart?.Invoke(startTime);
            return;
        }

        if (inOpenMapBlock)
        {
            if (LineParser.IsTokenLine(line, out string token))
            {
                currentMapEvent.Token = token;
                Log.Debug($"地圖 Token: {token}");
            }
            else if (LineParser.IsCurrentLevelLine(line, out int level))
            {
                currentMapEvent.Level = level;
                Log.Debug($"地圖 Level: {level}");
            }
            else if (LineParser.IsCurrentOpenMapIDLine(line, out int mapId))
            {
                currentMapEvent.MapId = mapId;
                Log.Debug($"地圖 ID: {mapId}");
            }

            // 假設區塊結束條件是遇到某個特定行
            if (LineParser.OpenMapEnd(line, out var endTime))
            {
                inOpenMapBlock = false;
                OnMapComplete?.Invoke(currentMapEvent);
            }
        }
    }
}
