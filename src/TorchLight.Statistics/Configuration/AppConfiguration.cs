namespace TorchLight.Statistics.Configuration;

/// <summary>
/// 應用程式配置
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// 遊戲日誌檔案可能的路徑
    /// </summary>
    public static readonly string[] CandidateLogPaths = 
    [        
        @"C:\Program Files (x86)\Torchlight Infinite\Game\UE_game\TorchLight\Saved\Logs\UE_game.log", 
        @"D:\Torchlight Infinite Game\UE_game\TorchLight\Saved\Logs\UE_game.log"
    ];

    /// <summary>
    /// 檔案監聽防抖動時間（毫秒）
    /// </summary>
    public const int FileWatcherDebounceMs = 500;

    /// <summary>
    /// 檔案輪詢間隔（秒）
    /// </summary>
    public const int FilePollingIntervalSeconds = 2;

    /// <summary>
    /// 是否從檔案末尾開始讀取
    /// </summary>
    public const bool StartFromFileEnd = true;

    /// <summary>
    /// 日誌時間格式
    /// </summary>
    public const string UnrealLogTimeFormat = "yyyy.MM.dd-HH.mm.ss:fff";

    /// <summary>
    /// 時區偏移（小時）- 用於轉換 UTC 到本地時間
    /// </summary>
    public const int TimeZoneOffsetHours = 8;
}
