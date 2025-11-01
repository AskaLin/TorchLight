using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using Serilog;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.Services;

/// <summary>
/// WebView2 通訊中樞 - 負責 C# 與 JavaScript 之間的雙向通訊
/// </summary>
public class WebViewHub
{
    private CoreWebView2 _coreWebView2;
    private Control _control;
    private bool _isInitialized = false;
    private readonly JsonSerializerOptions _jsonOpt = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly List<string> _denyEventNames = new()
    {
        // "bagSyncStatus",
        // "newMapRecord",
        // "itemPicked",
        // "currentMapUpdate",
        // "mapConfigUpdated",
        // "pickupStatisticsConfigUpdated",
        "logFileSize"
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
    public async Task SendMessageAsync(string eventName, object data = null)
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
            if(!_denyEventNames.Contains(eventName))
                Log.Debug("發送訊息到前端: {EventName}", eventName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "發送訊息到前端失敗: {EventName}", eventName);
        }
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
    public Task NotifyCurrentMapUpdateAsync(MapRecordViewModel mapData)
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

    /// <summary>
    /// 通知前端：拾取統計設定已更新
    /// </summary>
    public Task NotifyPickupStatisticsConfigUpdatedAsync(bool success, string message)
    {
        return SendMessageAsync("pickupStatisticsConfigUpdated", new
        {
            success,
            message
        });
    }

    /// <summary>
    /// 🆕 通知前端：log 檔案大小變更
    /// </summary>
    public Task NotifyLogFileSizeAsync(long fileSizeBytes)
    {
        // 格式化檔案大小
        string formattedSize;
        if (fileSizeBytes >= 1024 * 1024 * 1024) // >= 1GB
        {
            formattedSize = $"{fileSizeBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
        else // < 1GB，顯示為 MB
        {
            formattedSize = $"{fileSizeBytes / (1024.0 * 1024.0):F2} MB";
        }

        return SendMessageAsync("logFileSize", new
        {
            fileSizeBytes,
            formattedSize
        });
    }
}
