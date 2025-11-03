using Serilog;
using System.Text;
using TorchLight.Statistics.Configuration;
using TorchLight.Statistics.LogProcessor;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Services;
using TorchLight.Statistics.UI;

namespace TorchLight.Statistics
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
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

                // 🆕 載入應用程式設定
                Services.AppSettingsManager.LoadSettings();
                Log.Information("已載入應用程式設定");

                // 載入Config 初始化資料
                AppConfiguration.LoadConfigData();

                // 初始化地圖映射器
                MapInfoMapper.Initialize();

                // 初始化物品映射器
                ItemInfoMapper.Initialize();

                // 創建 WebViewHub（需要在 MainWindow 中初始化）
                var webViewHub = new WebViewHub();

                
                Log.Information("已載入 {ItemCount} 個物品定義", ItemInfoMapper.GetItemTable().Count);

                     
                var logProcessor = new GameLogProcessor();
                Log.Information("核心組件初始化完成");

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

                // 🆕 將 tail 傳遞給 logProcessor 以便在檢測到"已開啟日誌"時啟用監控
                logProcessor.SetFileTailWatcher(tail);

                tail.OnNewLine += logProcessor.ProcessLine;
                // 訂閱檔案大小變更事件
                tail.OnFileSizeChanged += (fileSize) =>
                {
                    // 通知前端檔案大小變更
                    _ = Task.Run(async () =>
                    {
                        await webViewHub.NotifyLogFileSizeAsync(fileSize);
                    });
                };

                // tail.Start();

                //測試用, 讀取現有日誌內容 進行處理
                using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader sr = new(fs, Encoding.UTF8);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    logProcessor.ProcessLine(line);
                }

                Log.Information("════════════════════════════════════════");
                Log.Information("監聽已啟動，等待遊戲事件...");
                Log.Information("提示：進入異界地圖後會自動開始統計拾取物品");
                Log.Information("════════════════════════════════════════");

                // 啟動 WebView2 UI
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);


                // 設定 GameLogProcessor 使用 WebViewHub
                logProcessor.SetWebViewHub(webViewHub);

                // 創建 MainWindow 並傳入 WebViewHub
                var mainWindow = new MainWindow(logProcessor.MapPickRecordManager, logProcessor, webViewHub);

                Application.Run(mainWindow);

                tail.Stop();
                Log.Information("程式已結束。感謝使用！");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "程式發生嚴重錯誤");
                MessageBox.Show($"程式發生嚴重錯誤：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
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
    }
}

