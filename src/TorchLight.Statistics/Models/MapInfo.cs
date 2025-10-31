using TorchLight.Statistics.Enums;

namespace TorchLight.Statistics.Models;

/// <summary>
/// 地圖資訊
/// </summary>
public class MapInfo
{
    public string Id { get; init; }
    public string Name { get; init; }
    public MapType Type { get; init; }
    public string FullPath { get; init; }
}
