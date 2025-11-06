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
        // 🆕 全域 SafeFileTailWatcher 引用，用於動態重啟
        private static SafeFileTailWatcher _tail;
        private static GameLogProcessor _logProcessor;
        private static WebViewHub _webViewHub;

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
                _webViewHub = new WebViewHub();


                Log.Information("已載入 {ItemCount} 個物品定義", ItemInfoMapper.GetItemTable().Count);


                _logProcessor = new GameLogProcessor();
                Log.Information("核心組件初始化完成");


                // 🆕 嘗試啟動日誌監聽器（如果路徑有效）
                var filePath = GetLogFilePath();

                //測試用, 讀取現有日誌內容 進行處理
                //using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                //using StreamReader sr = new(fs, Encoding.UTF8);
                //string line;
                //while ((line = sr.ReadLine()) != null)
                //{
                //    _logProcessor.ProcessLine(line);
                //}

                if (File.Exists(filePath))
                {
                    StartLogWatcher(filePath);
                }
                else
                {
                    Log.Warning("找不到日誌檔案: {FilePath}", filePath);
                    Log.Information("請在設定頁面中設定正確的日誌檔案路徑");
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
                _logProcessor.SetWebViewHub(_webViewHub);

                // 創建 MainWindow 並傳入 WebViewHub
                var mainWindow = new MainWindow(_logProcessor.MapPickRecordManager, _logProcessor, _webViewHub);

                // 🆕 訂閱日誌路徑變更事件
                Services.AppSettingsManager.OnLogPathChanged += HandleLogPathChanged;

                Application.Run(mainWindow);

                // 停止日誌監聽器
                StopLogWatcher();

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

        // ==================== 日誌監聽器管理 ====================

        /// <summary>
        /// 🆕 啟動日誌監聽器
        /// </summary>
        private static void StartLogWatcher(string filePath)
        {
            try
            {
                // 如果已經有監聽器在運行，先停止
                StopLogWatcher();

                Log.Information("啟動日誌監聽器: {FilePath}", filePath);

                _tail = new SafeFileTailWatcher(
                    filePath,
                    Encoding.UTF8,
                    TimeSpan.FromMilliseconds(AppConfiguration.FileWatcherDebounceMs),
                    TimeSpan.FromSeconds(AppConfiguration.FilePollingIntervalSeconds),
                    startFromEnd: AppConfiguration.StartFromFileEnd);

                // 將 tail 傳遞給 logProcessor 以便在檢測到"已開啟日誌"時啟用監控
                _logProcessor.SetFileTailWatcher(_tail);

                _tail.OnNewLine += _logProcessor.ProcessLine;

                // 訂閱檔案大小變更事件
                _tail.OnFileSizeChanged += (fileSize) =>
                {
                    // 通知前端檔案大小變更
                    _ = Task.Run(async () =>
                    {
                        await _webViewHub.NotifyLogFileSizeAsync(fileSize);
                    });
                };

                _tail.Start();

                Log.Information("日誌監聽器已啟動");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "啟動日誌監聽器失敗");
                MessageBox.Show($"無法啟動日誌監聽器：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 🆕 停止日誌監聽器
        /// </summary>
        private static void StopLogWatcher()
        {
            if (_tail != null)
            {
                try
                {
                    Log.Information("停止日誌監聽器");
                    _tail.Stop();
                    _tail.Dispose();
                    _tail = null;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "停止日誌監聽器時發生錯誤");
                }
            }
        }

        /// <summary>
        /// 🆕 處理日誌路徑變更事件
        /// </summary>
        private static void HandleLogPathChanged(string newPath)
        {
            Log.Information("檢測到日誌路徑變更: {NewPath}", newPath);

            if (string.IsNullOrWhiteSpace(newPath))
            {
                Log.Warning("新路徑為空，停止日誌監聽");
                StopLogWatcher();
                return;
            }

            if (!File.Exists(newPath))
            {
                Log.Warning("新路徑的檔案不存在: {NewPath}", newPath);
                StopLogWatcher();
                return;
            }

            // 重新啟動日誌監聽器
            Log.Information("重新啟動日誌監聽器");
            StartLogWatcher(newPath);
        }

        // ==================== 輔助方法 ====================

        /// <summary>
        /// 獲取遊戲日誌檔案路徑
        /// </summary>
        static string GetLogFilePath()
        {
            // 1. 優先從 appsettings.json 讀取使用者設定的路徑
            var settings = Services.AppSettingsManager.GetSettings();
            var configuredPath = settings?.Environment?.GameLogPath;

            if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            {
                Log.Information("使用設定檔中的日誌路徑");
                return configuredPath;
            }

            // 2. 如果設定檔中沒有或路徑無效，嘗試從預設候選路徑中尋找
            Log.Information("設定檔中未設定日誌路徑或路徑無效，嘗試從預設路徑搜尋...");
            foreach (var path in AppConfiguration.CandidateLogPaths)
            {
                if (File.Exists(path))
                {
                    Log.Information("在預設路徑中找到日誌檔案: {Path}", path);
                    return path;
                }
            }

            // 3. 如果都找不到，返回設定檔中的路徑（即使無效）或第一個預設路徑
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                Log.Warning("使用設定檔中的路徑（檔案不存在）: {Path}", configuredPath);
                return configuredPath;
            }

            Log.Warning("無法找到日誌檔案，返回預設路徑");
            return AppConfiguration.CandidateLogPaths[0];
        }
    }
}

