using Serilog;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Mapper;

namespace TorchLight.Statistics.Services;

/// <summary>
/// 處理地圖切換邏輯
/// </summary>
public class MapTransitionHandler
{
    private readonly MapPickRecordManager _mapPickRecordManager;
    private WebViewHub? _webViewHub;

    public MapTransitionHandler(MapPickRecordManager mapPickRecordManager)
    {
        _mapPickRecordManager = mapPickRecordManager ?? throw new ArgumentNullException(nameof(mapPickRecordManager));
    }

    /// <summary>
    /// 設定 WebViewHub（用於通知前端）
    /// </summary>
    public void SetWebViewHub(WebViewHub webViewHub)
    {
        _webViewHub = webViewHub;
    }

    /// <summary>
    /// 處理地圖切換事件
    /// </summary>
    public void HandleMapTransition(DateTime time, string fromPath, string toPath)
    {
        var fromMapName = MapInfoMapper.GetMapNameByFullPath(fromPath);
        var toMapName = MapInfoMapper.GetMapNameByFullPath(toPath);

        Log.Information($"{time:yyyy/MM/dd HH:mm:ss}\t從地圖 {fromMapName} 進入地圖 {toMapName}");

        _mapPickRecordManager.UpdateCurrentMapName(toMapName);

        // 從藏身處進入異界地圖
        if (MapInfoMapper.MapTypeCheck(fromPath, MapType.Hideout))
        {
            _mapPickRecordManager.StartMapRecord(toPath, toMapName, time);

            // 通知前端：進入新地圖
            if (_webViewHub != null)
            {
                _ = Task.Run(async () =>
                {
                    await _webViewHub.NotifyCurrentMapUpdateAsync(GetCurrentMapData());
                });
            }
        }
        // 從異界地圖返回藏身處
        else if (MapInfoMapper.MapTypeCheck(toPath, MapType.Hideout))
        {
            // _mapPickRecordManager.EndMapRecord(fromMapName, time);

            // 通知前端：地圖結算完成，新記錄已產生
            if (_webViewHub != null)
            {
                _ = Task.Run(async () =>
                {
                    await _webViewHub.NotifyNewMapRecordAsync();
                    await _webViewHub.NotifyCurrentMapUpdateAsync(GetCurrentMapData());
                });
            }
        }
    }

    /// <summary>
    /// 獲取當前地圖資料（用於通知前端）
    /// </summary>
    private object? GetCurrentMapData()
    {
        var currentRecord = _mapPickRecordManager.GetCurrentMapRecord();

        if (!_mapPickRecordManager.IsInNetherrealmMap)
        {
            // 避難所地圖
            return new
            {
                IsInMap = false,
                MapType = "Hideout",
                MapName = _mapPickRecordManager.CurrentMapName,
                RecordId = (Guid?)null,
                MapTicket = "",
                Compass = Array.Empty<string>(),
                Probe = "",
                StartTime = (DateTime?)null,
                Items = Array.Empty<object>()
            };
        }
        else if (currentRecord != null)
        {
            // 異界地圖 - 即時從 MapInfoMapper 獲取最新名稱
            return new
            {
                IsInMap = true,
                MapType = "Netherrealm",
                MapName = MapInfoMapper.GetMapName(currentRecord.Id),  // ✅ 即時獲取最新名稱
                RecordId = currentRecord.RecordId,
                MapTicket = currentRecord.MapTicket,
                Compass = currentRecord.Compass.Where(c => !string.IsNullOrEmpty(c)).ToArray(),
                Probe = currentRecord.Probe,
                StartTime = currentRecord.StartTime,
                Items = currentRecord.PickRecord?.Select(p => new
                {
                    p.Value.BaseId,
                    p.Value.Name,
                    p.Value.Total,
                    Slots = p.Value.Slots
                }).OrderByDescending(i => i.Total).ToArray() ?? Array.Empty<object>()
            };
        }
        else
        {
            // 在異界地圖但沒有記錄
            return new
            {
                IsInMap = true,
                MapType = "Netherrealm",
                MapName = _mapPickRecordManager.CurrentMapName,
                RecordId = (Guid?)null,
                MapTicket = "",
                Compass = Array.Empty<string>(),
                Probe = "",
                StartTime = (DateTime?)null,
                Items = Array.Empty<object>()
            };
        }
    }
}
