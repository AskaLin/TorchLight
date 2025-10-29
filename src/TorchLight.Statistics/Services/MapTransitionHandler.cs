namespace TorchLight.Statistics.Services;

/// <summary>
/// 處理地圖切換邏輯
/// </summary>
public class MapTransitionHandler
{
    private readonly MapPickRecordManager _mapPickRecordManager;

    public MapTransitionHandler(MapPickRecordManager mapPickRecordManager)
    {
        _mapPickRecordManager = mapPickRecordManager ?? throw new ArgumentNullException(nameof(mapPickRecordManager));
    }

    /// <summary>
    /// 處理地圖切換事件
    /// </summary>
    public void HandleMapTransition(DateTime time, string fromPath, string toPath)
    {
        var fromMapName = MapMapper.GetMapNameByFullPath(fromPath);
        var toMapName = MapMapper.GetMapNameByFullPath(toPath);

        Console.WriteLine($"{time:yyyy/MM/dd HH:mm:ss}\t從地圖 {fromMapName} 進入地圖 {toMapName}");

        _mapPickRecordManager.UpdateCurrentMapName(toMapName);

        // 從藏身處進入異界地圖
        if (MapMapper.IsHideoutMap(fromPath) && MapMapper.IsNetherrealmMap(toPath))
        {
            _mapPickRecordManager.StartMapRecord(toPath, toMapName, time);
        }
        // 從異界地圖返回藏身處
        else if (MapMapper.IsHideoutMap(toPath) && MapMapper.IsNetherrealmMap(fromPath))
        {
            _mapPickRecordManager.EndMapRecord(fromMapName, time);
        }
    }
}
