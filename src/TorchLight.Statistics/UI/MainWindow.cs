using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Serilog;
using System.Text.Json;
using TorchLight.Statistics.LogProcessor;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Services;

namespace TorchLight.Statistics.UI;

/// <summary>
/// WebView2 主視窗
/// </summary>
public class MainWindow : Form
{
    private readonly WebView2 _webView;
    private readonly MapPickRecordManager _mapPickRecordManager;
    private readonly GameLogProcessor _gameLogProcessor;
    private readonly WebViewHub _webViewHub;
    private bool _isInitialized = false;

    // 🆕 懸浮統計窗體
    private FloatingStatsWindow _floatingStatsWindow;

    public MainWindow(MapPickRecordManager mapPickRecordManager, GameLogProcessor gameLogProcessor, WebViewHub webViewHub)
    {
        _mapPickRecordManager = mapPickRecordManager ?? throw new ArgumentNullException(nameof(mapPickRecordManager));
        _gameLogProcessor = gameLogProcessor ?? throw new ArgumentNullException(nameof(gameLogProcessor));
        _webViewHub = webViewHub ?? throw new ArgumentNullException(nameof(webViewHub));

        // 設定視窗
        Text = "火炬之光無限 - 拾取物品統計工具";
        Width = 1200;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;

        // 創建 WebView2
        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(_webView);

        // 🆕 創建懸浮統計窗體
        InitializeFloatingStatsWindow();

        // 註冊遊戲日誌事件
        _gameLogProcessor.OnLogOpenedDetected += HandleLogOpenedDetected;
        _gameLogProcessor.OnBagSyncCompleted += HandleBagSyncCompleted;

        // 註冊地圖設定更新事件
        MapInfoMapper.OnConfigUpdated += HandleMapConfigUpdated;

        // 註冊物品設定更新事件
        ItemInfoMapper.OnConfigUpdated += HandlePickupStatisticsConfigUpdated;

        // 初始化 WebView2
        InitializeAsync();
    }

    // 🆕 初始化懸浮統計窗體
    private void InitializeFloatingStatsWindow()
    {
        _floatingStatsWindow = new FloatingStatsWindow();

        // 🆕 確保懸浮窗體不是主窗體的子窗體
        // 這樣它才能真正獨立顯示在最上層
        _floatingStatsWindow.Owner = null;

        // 預設關閉 顯示窗體
        _floatingStatsWindow.Hide();

        // 🆕 強制將懸浮窗體帶到前面
        _floatingStatsWindow.BringToFront();
        _floatingStatsWindow.Focus();

        // 定時更新統計數據
        var updateTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000  // 每秒更新一次
        };
        updateTimer.Tick += UpdateFloatingStats;
        updateTimer.Start();
    }

    // 🆕 更新懸浮窗體的統計數據
    private void UpdateFloatingStats(object sender, EventArgs e)
    {
        if (_floatingStatsWindow == null || _floatingStatsWindow.IsDisposed)
            return;

        try
        {
            var records = _mapPickRecordManager.MapRecords;
            // var currentMap = _mapPickRecordManager.IsInMap
            //    ? _mapPickRecordManager.CurrentMapName
            //    : "待機中";

            //// 計算總遊戲時間
            //var totalSeconds = records.Sum(r => (r.EndTime - r.StartTime).TotalSeconds);
            //var totalTime = TimeSpan.FromSeconds(totalSeconds).ToString(@"hh\:mm\:ss");

            //// 計算當前地圖拾取數
            //var currentMapPickCount = _mapPickRecordManager.IsInMap
            //    ? _mapPickRecordManager.GetCurrentMapRecord()?.PickRecord?.Sum(p => p.Value.Total) ?? 0
            //    : 0;

            var stats = new Dictionary<string, string>
            {
                { "地圖數", records.Count.ToString() },
                //{ "物品種類", records.Sum(r => r.PickRecord?.Count ?? 0).ToString() },
                //{ "總數量", records.Sum(r => r.PickRecord?.Select(p=>p.Value.Total).Sum() ?? 0).ToString() },
                //{ "遊戲時間", totalTime },
                //{ "當前地圖", currentMap },
                //{ "拾取數", currentMapPickCount.ToString() }
            };

            _floatingStatsWindow.UpdateStats(stats);

            // 🆕 更新監控物品數量
            UpdateWatchedItemsStats();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新懸浮統計窗體失敗");
        }
    }

    // 🆕 更新監控物品的統計數據
    private void UpdateWatchedItemsStats()
    {
        try
        {
            var allItems = ItemInfoMapper.GetAllItemConfigs();
            var watchedItems = allItems.Where(i => i.Watch).ToList();

            // ✅ 如果沒有監控物品，清空顯示
            if (watchedItems.Count == 0)
            {
                _floatingStatsWindow.UpdateWatchedItems(new List<WatchedItemInfo>());
                return;
            }

            var watchedItemInfos = new List<WatchedItemInfo>();

            // ✅ 為每個監控物品分別計算三個數量
            foreach (var watchedItem in watchedItems)
            {
                var itemId = watchedItem.Id;

                // 1. 背包內的總數量
                int bagTotal = _gameLogProcessor.BagInventoryManager.BagData
                    .Where(kvp => kvp.Key == itemId)
                    .Sum(kvp => kvp.Value.Total);

                // 2. 拾取的總數量（所有地圖記錄）
                int pickupTotal = _mapPickRecordManager.MapRecords
                    .SelectMany(r => r.PickRecord?.Values ?? Enumerable.Empty<Models.PickedItemDataModel>())
                    .Where(p => p.BaseId == itemId)
                    .Sum(p => p.Total);

                // 3. 當前地圖拾取的數量
                int currentMapPickup = 0;
                if (_mapPickRecordManager.IsInMap)
                {
                    var currentRecord = _mapPickRecordManager.GetCurrentMapRecord();
                    currentMapPickup = currentRecord?.PickRecord?.Values
                          .Where(p => p.BaseId == itemId)
                          .Sum(p => p.Total) ?? 0;
                }

                watchedItemInfos.Add(new WatchedItemInfo
                {
                    ItemId = itemId,
                    ItemName = watchedItem.Name,
                    BagTotal = bagTotal,
                    PickupTotal = pickupTotal + currentMapPickup, // 總計所有地圖的拾取數 在即時呈現時 應該加入目前的地圖拾取數
                    CurrentMapPickup = currentMapPickup
                });
            }

            _floatingStatsWindow.UpdateWatchedItems(watchedItemInfos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新監控物品統計失敗");
        }
    }

    // 🆕 顯示浮動窗體
    public void ShowFloatingWindow()
    {
        if (_floatingStatsWindow != null && !_floatingStatsWindow.IsDisposed)
        {
            _floatingStatsWindow.Show();
            _floatingStatsWindow.BringToFront();
            Log.Information("浮動統計窗體已顯示");
        }
    }

    // 🆕 隱藏浮動窗體
    public void HideFloatingWindow()
    {
        if (_floatingStatsWindow != null && !_floatingStatsWindow.IsDisposed)
        {
            _floatingStatsWindow.Hide();
            Log.Information("浮動統計窗體已隱藏");
        }
    }

    // 🆕 切換浮動窗體顯示狀態
    public bool ToggleFloatingWindow()
    {
        if (_floatingStatsWindow == null || _floatingStatsWindow.IsDisposed)
        {
            return false;
        }

        if (_floatingStatsWindow.Visible)
        {
            _floatingStatsWindow.Hide();
            Log.Information("浮動統計窗體已隱藏");
            return false;
        }
        else
        {
            _floatingStatsWindow.Show();
            _floatingStatsWindow.BringToFront();
            Log.Information("浮動統計窗體已顯示");
            return true;
        }
    }

    // 🆕 覆寫 Dispose 以確保懸浮窗體也被關閉
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _floatingStatsWindow?.Close();
            _floatingStatsWindow?.Dispose();
        }
        base.Dispose(disposing);
    }

    private async void InitializeAsync()
    {
        try
        {
            // 設定 WebView2 環境
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TorchLight.Statistics");

            var env = await CoreWebView2Environment.CreateAsync(
                       userDataFolder: userDataFolder
                    );

            await _webView.EnsureCoreWebView2Async(env);

            // 初始化 WebViewHub (傳入 Control 以便切換到 UI 執行緒)
            _webViewHub.Initialize(_webView.CoreWebView2, this);

            // 註冊 JavaScript 與 C# 的橋接
            RegisterJavaScriptBridge();

            // 載入前端頁面
            var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");

            if (File.Exists(wwwrootPath))
            {
                _webView.CoreWebView2.Navigate($"file:///{wwwrootPath.Replace("\\", "/")}");
            }
            else
            {
                // 開發模式：使用 Vite 開發伺服器
                _webView.CoreWebView2.Navigate("http://localhost:5173");
            }

            _isInitialized = true;
            Log.Information("WebView2 初始化完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "WebView2 初始化失敗");
            MessageBox.Show($"WebView2 初始化失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 註冊 JavaScript 與 C# 的橋接
    /// </summary>
    private void RegisterJavaScriptBridge()
    {
        // 從 JavaScript 呼叫 C# 方法
        _webView.CoreWebView2.AddHostObjectToScript("csharpApi", new WebViewApi(_mapPickRecordManager, _gameLogProcessor, this));

        // 啟用開發者工具（僅開發模式）
#if DEBUG
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#else
  _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif

        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
    }

    /// <summary>
    /// 從 C# 呼叫 JavaScript 方法
    /// </summary>
    public async Task CallJavaScriptAsync(string functionName, params object[] args)
    {
        if (!_isInitialized)
            return;

        try
        {
            var argsJson = args.Length > 0
                   ? string.Join(", ", args.Select(a => JsonSerializer.Serialize(a)))
              : "";

            var script = $"{functionName}({argsJson})";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "呼叫 JavaScript 方法失敗：{FunctionName}", functionName);
        }
    }

    /// <summary>
    /// 通知前端：新地圖記錄
    /// </summary>
    public async Task NotifyNewMapRecord()
    {
        if (_isInitialized)
        {
            await CallJavaScriptAsync("window.onNewMapRecord");
        }
    }

    /// <summary>
    /// 通知前端：物品拾取
    /// </summary>
    public async Task NotifyItemPicked(string itemName, int quantity)
    {
        if (_isInitialized)
        {
            await CallJavaScriptAsync("window.onItemPicked", itemName, quantity);
        }
    }

    /// <summary>
    /// 處理 "已開啟日誌" 事件
    /// </summary>
    private async void HandleLogOpenedDetected()
    {
        if (_isInitialized)
        {
            // ❌ 移除：不再需要通知 logMonitoringStatus
            // await _webViewHub.NotifyLogMonitoringStatusAsync("監控日誌中");
            Log.Information("已檢測到：已開啟日誌");
        }
    }

    /// <summary>
    /// 處理背包同步完成事件
    /// </summary>
    private async void HandleBagSyncCompleted()
    {
        if (_isInitialized)
        {
            await _webViewHub.NotifyBagSyncStatusAsync(DateTime.Now);
            Log.Information("已通知前端：背包同步完成");
        }
    }

    /// <summary>
    /// 處理地圖設定更新事件
    /// </summary>
    private async void HandleMapConfigUpdated(bool success, string message)
    {
        if (_isInitialized)
        {
            await _webViewHub.NotifyMapConfigUpdatedAsync(success, message);

            // ✅ 如果玩家正在地圖中，立即更新當前地圖資訊
            if (success && _mapPickRecordManager.IsInMap)
            {
                var currentMapData = _gameLogProcessor.GetCurrentMapData();
                await _webViewHub.NotifyCurrentMapUpdateAsync(currentMapData);
                Log.Information("當前地圖資訊已更新");
            }

            Log.Information("地圖設定更新通知已發送 - Success: {Success}, Message: {Message}", success, message);
        }
    }

    /// <summary>
    /// 處理拾取統計設定更新事件
    /// </summary>
    private async void HandlePickupStatisticsConfigUpdated(bool success, string message)
    {
        if (_isInitialized)
        {
            await _webViewHub.NotifyPickupStatisticsConfigUpdatedAsync(success, message);
            Log.Information("拾取統計設定更新通知已發送 - Success: {Success}, Message: {Message}", success, message);
        }
    }

    /// <summary>
    /// 通知前端背包同步狀態
    /// </summary>
    public async Task NotifyBagSyncAsync()
    {
        if (_isInitialized)
        {
            await _webViewHub.NotifyBagSyncStatusAsync(DateTime.Now);
        }
    }
}
