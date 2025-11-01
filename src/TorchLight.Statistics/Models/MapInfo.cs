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

    public string RealName(int mapTickerId)
    {
        var fixName = Name;

        if (Id == "GeBuLinCunLuo01")
        {
            fixName = mapTickerId switch
            {
                400006 or 400007 => "亞人村落",
                400027 or 400028 => "災厄之林",
                _ => fixName,
            };
        }
        else if (Id == "JH_ShengDeLanXiuDaoYuan000")
        {
            fixName = mapTickerId == 400014 ? "懺罪小教堂" : "懺悔學院";            
        }


        return mapTickerId switch
        {
            // k7
            // 400006 or 400014 or 400021 or 400027 or 4000032 => Name,

            // k8
            400007 => $"滾燙的{fixName}",
            400015 => $"徹骨的{fixName}",
            400022 => $"柔軟的{fixName}",
            400028 => $"漆黑的{fixName}",
            400033 => $"耀眼的{fixName}",

            _ => $"{fixName}",
        };
    }
}
