using System.Text;
using Serilog;
using TorchLight.Statistics;
using TorchLight.Statistics.Configuration;

// 初始化 Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/torchlight-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("╔════════════════════════════════════════╗");
Log.Information("║  火炬之光無限 - 拾取物品統計工具  ║");
Log.Information("║  Torchlight Infinite Item Tracker      ║");
Log.Information("╚════════════════════════════════════════╝");
Log.Information("");

try
{
    // 初始化核心組件
    Log.Information("正在初始化...");
    var itemTable = ItemIdTable.GetItemTable();
    Log.Information("已載入 {ItemCount} 個物品定義", itemTable.Count);

    var lineParser = new LineParser(itemTable);
    var itemChangeProcessor = new ItemChangeBlockProcessor();
    var logProcessor = new GameLogProcessor(itemTable, lineParser, itemChangeProcessor);
    Log.Information("核心組件初始化完成");

    // 建立選單管理器
    var menuManager = new MenuManager(logProcessor.MapPickRecordManager);

    // 設定日誌檔案路徑
    var filePath = GetLogFilePath();
    if (!File.Exists(filePath))
    {
        Log.Warning("找不到日誌檔案: {FilePath}", filePath);
        Log.Information("請確認遊戲是否已安裝，或手動設定日誌路徑");
        Log.Information("按下 Enter 結束程式...");
        Console.ReadLine();
        return;
    }

    Log.Information("日誌檔案: {FilePath}", filePath);

    // 啟動日誌監聽器
    using var tail = new SafeFileTailWatcher(
        filePath,
        Encoding.UTF8,
        TimeSpan.FromMilliseconds(AppConfiguration.FileWatcherDebounceMs),
        TimeSpan.FromSeconds(AppConfiguration.FilePollingIntervalSeconds),
        startFromEnd: AppConfiguration.StartFromFileEnd);

    tail.OnNewLine += logProcessor.ProcessLine;
    tail.Start();

    Log.Information("════════════════════════════════════════");
    Log.Information("監聽已啟動，等待遊戲事件...");
    Log.Information("提示：進入異界地圖後會自動開始統計拾取物品");
    Log.Information("════════════════════════════════════════");
    Log.Information("按 'M' 或 'm' 開啟選單，按 Enter 結束程式");

    // 主迴圈：監聽鍵盤輸入
    while (true)
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.M)
            {
                // 進入選單模式
                while (menuManager.IsRunning)
                {
                    menuManager.ShowMainMenu();
                    var input = Console.ReadLine();
                    menuManager.HandleInput(input);
                }

                // 離開選單，顯示提示
                Console.WriteLine("\n返回監聽模式...");
                Log.Information("按 'M' 或 'm' 開啟選單，按 Enter 結束程式");
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                // 結束程式
                break;
            }
        }

        Thread.Sleep(100); // 避免佔用過多 CPU
    }

    tail.Stop();
    Log.Information("程式已結束。感謝使用！");
}
catch (Exception ex)
{
    Log.Fatal(ex, "程式發生嚴重錯誤");
    Log.Information("按下 Enter 結束程式...");
    Console.ReadLine();
}
finally
{
    Log.CloseAndFlush();
}

// ==================== 輔助方法 ====================

/// <summary>
/// 獲取遊戲日誌檔案路徑
/// </summary>
static string GetLogFilePath()
{
    foreach (var path in AppConfiguration.CandidateLogPaths)
    {
        if (File.Exists(path))
        {
            return path;
        }
    }

    // 如果都找不到，返回第一個作為預設值
    return AppConfiguration.CandidateLogPaths[0];
}

