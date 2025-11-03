using System.Text.Json.Serialization;
using TorchLight.Statistics.Enums;

namespace TorchLight.Statistics.Models;

/// <summary>
/// 地圖設定項目
/// </summary>
public class MapIdConfig
{
    /// <summary>
    /// 地圖ID
    /// 1091000  異界地圖
    /// 2,3位 [09] 代表 level, 9 = 8-0, 10 = 8-1, 11 = 8-2....
    /// 4,5位 [10] 代表區域, 10 = 冰, 11 = 炎, 12 = 鋼, 13 = 雷, 14 = 火
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    public int Level
    {
        get => int.Parse(Id.ToString().Substring(1, 2));
    }
    
    /// <summary>
    /// 地圖名稱
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
    /// <summary>
    /// 地圖類型
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MapType Type { get; set; }

    public string GetDisplayName()
    {
        if (Type == MapType.Netherrealm && Id > 1090000)
        {
            var levelStr = GetLevel();
            // 1120000
            var prefix = Id < 1120000 ? GetLevelPrefixName() :
                         Id < 1130000 ? "幽邃的" : string.Empty; // > 113 應該是深空, 但是我太廢沒打過
            return $"{levelStr} {prefix}{Name}";
        }
        return Name;
    }

    private string GetLevel()
    {
        return Level switch
        {
            6 => "7-0",
            7 => "7-1",
            8 => "7-2",
            9 => "8-0",
            10 => "8-1",
            11 => "8-2",
            12 => "U8",
            _ => string.Empty
        };
    }
    private string GetLevelPrefixName()
    {
        return Id.ToString().Substring(3, 2) switch
        {
            "10" => "滾燙的",
            "11" => "徹骨的",
            "12" => "柔軟的",
            "13" => "漆黑的",
            "14" => "耀眼的",
            _ => string.Empty
        };
    }
}
