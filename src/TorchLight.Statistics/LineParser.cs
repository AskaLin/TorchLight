using Serilog;
using System;
using TorchLight.Statistics.Configuration;
using TorchLight.Statistics.LogProcessor;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics;

/// <summary>
/// 日誌行解析器 - 負責解析遊戲日誌的各種格式
/// </summary>
public partial class LineParser()
{
    private static readonly Dictionary<int, ItemModel> _itemTable = ItemInfoMapper.GetItemTable();

    /// <summary>
    /// 需要忽略的頁面ID（裝備欄與技能欄）
    /// </summary>
    private static readonly HashSet<int> _ignorePageIds = [100, 101];

    /// <summary>
    /// 標記是否正在進行背包初始化
    /// </summary>
    private static bool _isInitializingBag = false;

    #region 日誌行類型判斷

    /// <summary>
    /// 是否為登入開始的日誌
    /// </summary>
    public static bool IsLoginStart(string line) => line.Contains("LuaLoading@ LoadUILogic STT!");

    /// <summary>
    /// 是否為已開啟日誌的訊息
    /// </summary>
    public static bool IsLogOpenedMessage(string line) => line.Contains("MsgMgr@:Show MsgValue = 已開啟日誌");

    /// <summary>
    /// 是否為關閉遊戲的Log
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static bool CloseGame(string line) => line.Contains("LifeCycle@:TorchLight-GameDestroy");

    /// <summary>
    /// 是不是開啟新地圖的Log
    /// </summary>
    /// <param name="line"></param>
    /// <param name="datetime"></param>
    /// <returns></returns>
    public static bool OpenMapStart(string line, out DateTime datetime)
    {
        datetime = DateTime.MinValue;
        if (line.Contains("----Socket RecvMessage STT----Spv3Open----"))
        {
            var match = LineRegex.GetDateTimeValue().Match(line);
            if (match.Success)
            {
                Log.Debug("開始開新圖");
                datetime = ParseUnrealDateTime(match.Groups[1].Value);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 比對特定字串, 並返回時間, 一般用於區域開始或結束的日誌行
    /// </summary>
    /// <param name="line"></param>
    /// <param name="contains"></param>
    /// <param name="datetime"></param>
    /// <returns></returns>
    public static bool GetLineDateTime(string line, string contains, out DateTime datetime)
    {
        datetime = DateTime.MinValue;
        if (line.Contains(contains))
        {
            var match = LineRegex.GetDateTimeValue().Match(line);
            if (match.Success)
            {             
                datetime = ParseUnrealDateTime(match.Groups[1].Value);
                return true;
            }
        }
        return false;
    }

    public static bool OpenMapEnd(string line, out DateTime datetime)
    {
        datetime = DateTime.MinValue;
        if (line.Contains("----Socket RecvMessage End----"))
        {
            var match = LineRegex.GetDateTimeValue().Match(line);
            if (match.Success)
            {
                Log.Debug("結束開新圖");
                datetime = ParseUnrealDateTime(match.Groups[1].Value);
                return true;
            }
        }
        return false;
    }
    public static bool OpenMapEnd(string line, string contains, out DateTime datetime)
    {        
        datetime = DateTime.MinValue;
        if (line.Contains(contains))
        {
            var match = LineRegex.GetDateTimeValue().Match(line);
            if (match.Success)
            {
                Log.Debug("結束開新圖");
                datetime = ParseUnrealDateTime(match.Groups[1].Value);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 檢查是否為地圖 Token 行
    /// </summary>
    /// <param name="line"></param>
    /// <param name="openMapFlag"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static bool IsTokenLine(string line, out string token)
    {
        token = string.Empty;
        if (line.Contains("+TokenKey ["))
        {
            var match = LineRegex.GetCellValue().Match(line);
            if (match.Success)
            {
                token = match.Groups[1].Value;
                return true;
            }
        }
        return false;
    }
    public static bool IsTokenLine(string line, string contains, out string token)
    {
        token = string.Empty;
        if (line.Contains(contains))
        {
            var match = LineRegex.GetCellValue().Match(line);
            if (match.Success)
            {
                token = match.Groups[1].Value;
                return true;
            }
        }
        return false;
    }

    public static bool IsCurrentLevelLine(string line, out int level)
    {
        level = 0;
        if (line.Contains("+CurrentLevel ["))
        {
            var match = LineRegex.GetCellValue().Match(line);
            if (match.Success)
            {
                level = int.Parse(match.Groups[1].Value);
                return true;
            }
        }
        return false;
    }

    public static bool IsCurrentOpenMapIDLine(string line, out int mapId)
    {
        mapId = 0;
        if (line.Contains("+CurrentOpenMapID ["))
        {
            var match = LineRegex.GetCellValue().Match(line);
            if (match.Success)
            {
                mapId = int.Parse(match.Groups[1].Value);
                return true;
            }
        }
        return false;
    }
    public static bool IsCurrentOpenMapIDLine(string line, string contains, out int mapId)
    {
        mapId = 0;
        if (line.Contains(contains))
        {
            var match = LineRegex.GetCellValue().Match(line);
            if (match.Success)
            {
                mapId = int.Parse(match.Groups[1].Value);
                return true;
            }
        }
        return false;
    }

    public static bool IsBagMgr(string line, string contains, out ItemChangeEvent itemChangeEvent)
    {
        itemChangeEvent = null;
        if (line.Contains(contains))
        {
            var match = LineRegex.BagItemLine().Match(line);
            if (!match.Success) return false;

            var time = ParseUnrealDateTime(match.Groups["time"].Value);

            itemChangeEvent = new ItemChangeEvent
            {
                Time = time,
                PageId = int.Parse(match.Groups["page"].Value),
                SlotId = int.Parse(match.Groups["slot"].Value),
                ConfigBaseId = int.Parse(match.Groups["config"].Success ? match.Groups["config"].Value : "0"),
                Num = int.Parse(match.Groups["num"].Success ? match.Groups["num"].Value : "0"),
                // ProtoName = _currentProtoName,
                Action = match.Groups["action"].Value
            };
            return true;

        }
        return false;
    }

    public static DateTime GetLineDateTime(string line)
    {
        var match = LineRegex.GetDateTimeValue().Match(line);
        if (match.Success)
        {
            Log.Debug("開始開新圖");
            return ParseUnrealDateTime(match.Groups[1].Value);
        }
        return DateTime.MinValue;
    }

    /// <summary>
    /// 是否為初始化完成的日誌（已廢棄，改用 CheckBagInitializationState）
    /// </summary>
    //[Obsolete("請使用 CheckBagInitializationState 來判斷初始化狀態")]
    //public bool IsInitFinished(string line) => line.Contains("LuaLoading@ NetData _LoadFunctionNetData Progress = 1.0");

    /// <summary>
    /// 檢查並更新背包初始化狀態
    /// </summary>
    /// <param name="line">當前日誌行</param>
    /// <returns>
    /// (isInitLine, shouldProcess, isComplete, isFirstInit) 
    /// - isInitLine: 是否為初始化行
    /// - shouldProcess: 是否應該處理這行
    /// - isComplete: 初始化是否完成
    /// - isFirstInit: 是否為第一次開始初始化
    /// </returns>
    public static (bool isInitLine, bool shouldProcess, bool isComplete, bool isFirstInit) CheckBagInitializationState(string line)
    {
        bool hasInitBagData = line.Contains("BagMgr@:InitBagData");
        bool isIgnored = IsIgnoredPage(line);

        if (!_isInitializingBag)
        {
            // 尚未開始初始化
            if (hasInitBagData && !isIgnored)
            {
                // 開始初始化
                _isInitializingBag = true;
                return (isInitLine: true, shouldProcess: true, isComplete: false, isFirstInit: true);
            }
            return (isInitLine: false, shouldProcess: false, isComplete: false, isFirstInit: false);
        }
        else
        {
            // 正在初始化中
            if (hasInitBagData && !isIgnored)
            {
                // 繼續初始化
                return (isInitLine: true, shouldProcess: true, isComplete: false, isFirstInit: false);
            }
            else
            {
                // 初始化結束（下一行不是初始化行）
                _isInitializingBag = false;
                return (isInitLine: false, shouldProcess: false, isComplete: true, isFirstInit: false);
            }
        }
    }

    ///// <summary>
    ///// 是否為初始化背包物品的日誌（保留用於相容性，但建議使用 CheckBagInitializationState）
    ///// </summary>
    //public bool IsInitBagItemData(string line) => line.Contains("BagMgr@:InitBagData") && !IsIgnoredPage(line);

    ///// <summary>
    ///// 是否為修改背包物品的日誌
    ///// </summary>
    //public bool IsModfyBagItemData(string line) => line.Contains("BagMgr@:Modfy BagItem") && !IsIgnoredPage(line);

    ///// <summary>
    ///// 是否為刪除背包物品的日誌
    ///// </summary>
    //public bool IsDeleteBagItemData(string line) => line.Contains("BagMgr@:RemoveBagItem") && !IsIgnoredPage(line);

    ///// <summary>
    ///// 是否為地圖切換的日誌
    ///// </summary>
    //public static bool IsMoveMap(string line) => line.Contains("PageApplyBase@ _UpdateGameEnd: LastSceneName = World'/Game/Art/Maps/");

    /// <summary>
    /// 重置初始化狀態（登入時使用）
    /// </summary>
    public static void ResetInitializationState()
    {
        _isInitializingBag = false;
    }

    #endregion

    #region 數據解析

    /// <summary>
    /// 解析地圖切換資料
    /// </summary>
    /// <returns>時間、來源地圖、目標地圖、是否成功</returns>
    public static (DateTime time, string fromPath, string toPath, bool success) GetMapPathData(string line)
    {
        var match = LineRegex.MapLine().Match(line);
        if (!match.Success)
        {
            Log.Warning($"未能解析地圖切換資料: {line}");
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
    public static ItemModel GetItemData(string line)
    {
        var match = LineRegex.BagItemLine().Match(line);
        if (!match.Success)
        {
            throw new FormatException($"無法解析物品資料: {line}");
        }

        var configBaseId = Convert.ToInt32(match.Groups["config"].Value);
        var itemName = _itemTable.TryGetValue(configBaseId, out var item) ? item.Name : $"未知物品({configBaseId})";

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
    public static DateTime ParseUnrealDateTime(string timeStr)
    {
        var dt = DateTime.ParseExact(timeStr, AppConfiguration.UnrealLogTimeFormat, null);
        return dt.AddHours(AppConfiguration.TimeZoneOffsetHours);
    }

    /// <summary>
    /// 判斷是否為需要忽略的頁面
    /// </summary>
    private static bool IsIgnoredPage(string line)
    {
        // 快速檢查：尋找 "PageId = XXX" 模式
        var pageIdIndex = line.IndexOf("PageId = ", StringComparison.Ordinal);
        if (pageIdIndex == -1) return false;

        var valueStart = pageIdIndex + 9; // "PageId = ".Length
        var valueEnd = line.IndexOf(' ', valueStart);
        if (valueEnd == -1) valueEnd = line.Length;

        var pageIdStr = line[valueStart..valueEnd];
        if (int.TryParse(pageIdStr, out var pageId))
        {
            return _ignorePageIds.Contains(pageId);
        }

        return false;
    }
    #endregion
}
