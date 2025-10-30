namespace TorchLight.Statistics.Core;

/// <summary>
/// 地圖類型枚舉
/// </summary>
public enum MapType
{
    /// <summary>
    /// 未知地圖
    /// </summary>
    Unknown,

    /// <summary>
    /// 藏身處
    /// </summary>
    Hideout,

    /// <summary>
    /// 異界地圖（可統計拾取）
    /// </summary>
    Netherrealm
}

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
