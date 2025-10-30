using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using Serilog;

namespace TorchLight.Statistics.Services;

/// <summary>
/// WebView2 通訊中樞 - 負責 C# 與 JavaScript 之間的雙向通訊
/// </summary>
public class WebViewHub
{
    private CoreWebView2? _coreWebView2;
    private Control? _control;
    private bool _isInitialized = false;
    private readonly JsonSerializerOptions _jsonOpt = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// 初始化 WebView2 通訊
    /// </summary>
    public void Initialize(CoreWebView2 coreWebView2, Control control)
    {
        _coreWebView2 = coreWebView2;
        _control = control;
        _isInitialized = true;
        Log.Information("WebViewHub 已初始化");
    }

    /// <summary>
    /// 發送訊息到前端
    /// </summary>
    public async Task SendMessageAsync(string eventName, object? data = null)
    {
        if (!_isInitialized || _coreWebView2 == null || _control == null)
        {
            Log.Warning("WebViewHub 尚未初始化，無法發送訊息");
            return;
        }

        try
        {
            var message = new
            {
                type = eventName,
                data,
                timestamp = DateTime.Now
            };

            var json = JsonSerializer.Serialize(message, _jsonOpt);

            // 確保在 UI 執行緒上執行
            if (_control.InvokeRequired)
            {
                await Task.Run(() => _control.Invoke(async () =>
                {
                    var script = $"window.postMessage({json}, '*')";
                    await _coreWebView2.ExecuteScriptAsync(script);
                }));
            }
            else
            {
                var script = $"window.postMessage({json}, '*')";
                await _coreWebView2.ExecuteScriptAsync(script);
            }

            Log.Debug("發送訊息到前端: {EventName}", eventName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "發送訊息到前端失敗: {EventName}", eventName);
        }
    }

    /// <summary>
    /// 通知前端：日誌監控狀態變更
    /// </summary>
    public Task NotifyLogMonitoringStatusAsync(string status, DateTime? syncTime = null)
    {
        return SendMessageAsync("logMonitoringStatus", new
        {
            status,
            syncTime = syncTime ?? DateTime.Now
        });
    }

    /// <summary>
    /// 通知前端：背包同步狀態
    /// </summary>
    public Task NotifyBagSyncStatusAsync(DateTime syncTime)
    {
        return SendMessageAsync("bagSyncStatus", new
        {
            syncTime
        });
    }

    /// <summary>
    /// 通知前端：新地圖記錄
    /// </summary>
    public Task NotifyNewMapRecordAsync()
    {
        return SendMessageAsync("newMapRecord");
    }

    /// <summary>
    /// 通知前端：物品拾取
    /// </summary>
    public Task NotifyItemPickedAsync(string itemName, int quantity)
    {
        return SendMessageAsync("itemPicked", new
        {
            itemName,
            quantity
        });
    }

    /// <summary>
    /// 通知前端：當前地圖資訊更新
    /// </summary>
    public Task NotifyCurrentMapUpdateAsync(object? mapData)
    {
        return SendMessageAsync("currentMapUpdate", mapData);
    }

    /// <summary>
    /// 通知前端：地圖設定已更新
    /// </summary>
    public Task NotifyMapConfigUpdatedAsync(bool success, string message)
    {
        return SendMessageAsync("mapConfigUpdated", new
        {
            success,
            message
        });
    }
}
