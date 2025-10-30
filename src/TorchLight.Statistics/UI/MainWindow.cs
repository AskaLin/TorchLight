using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Serilog;
using System.Text.Json;
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
    private bool _isInitialized = false;

    public MainWindow(MapPickRecordManager mapPickRecordManager, GameLogProcessor gameLogProcessor)
    {
        _mapPickRecordManager = mapPickRecordManager ?? throw new ArgumentNullException(nameof(mapPickRecordManager));
      _gameLogProcessor = gameLogProcessor ?? throw new ArgumentNullException(nameof(gameLogProcessor));

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

        // 初始化 WebView2
   InitializeAsync();
    }

    private async void InitializeAsync()
    {
    try
        {
     // 設定 WebView2 環境
      var userDataFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
  "TorchLight.Statistics"
   );

  var env = await CoreWebView2Environment.CreateAsync(
             userDataFolder: userDataFolder
          );

            await _webView.EnsureCoreWebView2Async(env);

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
}
