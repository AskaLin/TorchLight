using TorchLight.Statistics.Configuration;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics;

/// <summary>
/// 日誌行解析器 - 負責解析遊戲日誌的各種格式
/// </summary>
public partial class LineParser
{
    private readonly Dictionary<int, string> _itemIdTable;

    /// <summary>
    /// 需要忽略的頁面ID（裝備欄與技能欄）
    /// </summary>
    private readonly HashSet<int> _ignorePageIds = [100, 101];

    public LineParser(Dictionary<int, string> itemIdTable)
    {
        _itemIdTable = itemIdTable ?? throw new ArgumentNullException(nameof(itemIdTable));
    }

    #region 日誌行類型判斷

    /// <summary>
    /// 是否為登入開始的日誌
    /// </summary>
    public bool IsLoginStart(string line) => line.Contains("LuaLoading@ LoadUILogic STT!");

    /// <summary>
    /// 是否為初始化完成的日誌
    /// </summary>
    public bool IsInitFinished(string line) => line.Contains("LuaLoading@ NetData _LoadFunctionNetData Progress = 1.0");

    /// <summary>
    /// 是否為初始化背包物品的日誌
    /// </summary>
    public bool IsInitBagItemData(string line) => line.Contains("BagMgr@:InitBagData") && !IsIgnoredPage(line);

    /// <summary>
    /// 是否為修改背包物品的日誌
    /// </summary>
    public bool IsModfyBagItemData(string line) => line.Contains("BagMgr@:Modfy BagItem") && !IsIgnoredPage(line);

    /// <summary>
    /// 是否為刪除背包物品的日誌
    /// </summary>
    public bool IsDeleteBagItemData(string line) => line.Contains("BagMgr@:RemoveBagItem") && !IsIgnoredPage(line);

    /// <summary>
    /// 是否為地圖切換的日誌
    /// </summary>
    public bool IsMoveMap(string line) => line.Contains("PageApplyBase@ _UpdateGameEnd: LastSceneName = World'/Game/Art/Maps/");

    #endregion

    #region 數據解析

    /// <summary>
    /// 解析地圖切換資料
    /// </summary>
    /// <returns>時間、來源地圖、目標地圖、是否成功</returns>
    public (DateTime time, string fromPath, string toPath, bool success) GetMapPathData(string line)
    {
        var match = LineRegex.MapLine().Match(line);
        if (!match.Success)
        {
            Console.WriteLine($"[警告] 未能解析地圖切換資料: {line}");
            return (DateTime.MinValue, string.Empty, string.Empty, false);
        }

        var time = ParseUnrealDateTime(match.Groups["time"].Value);
        var fromPath = match.Groups["from"].Value;
        var toPath = match.Groups["to"].Value;

        return (time, fromPath, toPath, true);
    }

    /// <summary>
    /// 解析物品資料
    /// </summary>
    public ItemModel GetItemData(string line)
    {
        var match = LineRegex.BagItemLine().Match(line);
        if (!match.Success)
        {
            throw new FormatException($"無法解析物品資料: {line}");
        }

        var configBaseId = Convert.ToInt32(match.Groups["config"].Value);
        var itemName = GetItemName(configBaseId);

        return new ItemModel
        {
            PageId = Convert.ToInt16(match.Groups["page"].Value),
            SoltId = Convert.ToInt16(match.Groups["slot"].Value),
            ConfigBaseId = configBaseId,
            Num = Convert.ToInt16(match.Groups["num"].Value),
            Time = ParseUnrealDateTime(match.Groups["time"].Value),
            Name = itemName
        };
    }

    #endregion

    #region 輔助方法

    /// <summary>
    /// 將 Unreal 日誌時間格式轉換為 DateTime
    /// </summary>
    private static DateTime ParseUnrealDateTime(string timeStr)
    {
        var dt = DateTime.ParseExact(timeStr, AppConfiguration.UnrealLogTimeFormat, null);
        return dt.AddHours(AppConfiguration.TimeZoneOffsetHours);
    }

    /// <summary>
    /// 獲取物品名稱
    /// </summary>
    private string GetItemName(int configBaseId)
    {
        return _itemIdTable.TryGetValue(configBaseId, out var name)
             ? name
                 : $"未知物品({configBaseId})";
    }

    /// <summary>
    /// 判斷是否為需要忽略的頁面
    /// </summary>
    private bool IsIgnoredPage(string line)
    {
        // 快速檢查：尋找 "PageId = XXX" 模式
        var pageIdIndex = line.IndexOf("PageId = ", StringComparison.Ordinal);
        if (pageIdIndex == -1) return false;

        var valueStart = pageIdIndex + 9; // "PageId = ".Length
        var valueEnd = line.IndexOf(' ', valueStart);
        if (valueEnd == -1) valueEnd = line.Length;

        var pageIdStr = line.Substring(valueStart, valueEnd - valueStart);
        if (int.TryParse(pageIdStr, out var pageId))
        {
            return _ignorePageIds.Contains(pageId);
        }

        return false;
    }

    #endregion
}
