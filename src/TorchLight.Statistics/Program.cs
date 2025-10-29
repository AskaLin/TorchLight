using System.Text;
using TorchLight.Statistics;
using TorchLight.Statistics.Configuration;

Console.WriteLine("╔════════════════════════════════════════╗");
Console.WriteLine("║  火炬之光無限 - 拾取物品統計工具       ║");
Console.WriteLine("║  Torchlight Infinite Item Tracker      ║");
Console.WriteLine("╚════════════════════════════════════════╝");
Console.WriteLine();

try
{
    // 初始化核心組件
    Console.WriteLine("正在初始化...");
    var itemIdTable = ItemIdTable.GetIdTable();
    Console.WriteLine($"✓ 已載入 {itemIdTable.Count} 個物品定義");

    var lineParser = new LineParser(itemIdTable);
    var itemChangeProcessor = new ItemChangeBlockProcessor();
    var logProcessor = new GameLogProcessor(itemIdTable, lineParser, itemChangeProcessor);
    Console.WriteLine("✓ 核心組件初始化完成");

    // 設定日誌檔案路徑
    var filePath = GetLogFilePath();
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"\n[警告] 找不到日誌檔案: {filePath}");
        Console.WriteLine("請確認遊戲是否已安裝，或手動設定日誌路徑。");
        Console.WriteLine("\n按下 Enter 結束程式...");
        Console.ReadLine();
        return;
    }

    Console.WriteLine($"✓ 日誌檔案: {filePath}");
    Console.WriteLine();

    // 啟動日誌監聽器
    using var tail = new SafeFileTailWatcher(
        filePath,
        Encoding.UTF8,
        TimeSpan.FromMilliseconds(AppConfiguration.FileWatcherDebounceMs),
        TimeSpan.FromSeconds(AppConfiguration.FilePollingIntervalSeconds),
        startFromEnd: AppConfiguration.StartFromFileEnd);

    tail.OnNewLine += logProcessor.ProcessLine;
    tail.Start();

    Console.WriteLine("════════════════════════════════════════");
    Console.WriteLine("監聽已啟動，等待遊戲事件...");
    Console.WriteLine("提示：進入異界地圖後會自動開始統計拾取物品");
    Console.WriteLine("════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("按下 Enter 鍵以停止監聽並結束程式");
    Console.WriteLine();

    Console.ReadLine();

    tail.Stop();
    Console.WriteLine("\n程式已結束。感謝使用！");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[嚴重錯誤] {ex.Message}");
    Console.WriteLine($"詳細資訊: {ex}");
    Console.WriteLine("\n按下 Enter 結束程式...");
    Console.ReadLine();
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

