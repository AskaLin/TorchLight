using System.Collections.Concurrent;
using Serilog;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.Services;

/// <summary>
/// WebView 通知節流器 - 減少前端通知頻率
/// </summary>
public class WebViewNotificationThrottle : IDisposable
{
    private readonly WebViewHub _webViewHub;
    private readonly ConcurrentQueue<NotificationItem> _notificationQueue = new();
    private readonly System.Threading.Timer _flushTimer;
    private readonly TimeSpan _flushInterval;
    private readonly object _lock = new();
    private bool _disposed;

    // 防抖動相關
    private System.Threading.Timer _currentMapUpdateDebouncer;
    private readonly TimeSpan _currentMapUpdateDelay = TimeSpan.FromMilliseconds(500);
    private MapRecordViewModel _pendingCurrentMapData;

    // 🆕 背包同步防抖動
    private System.Threading.Timer _bagSyncDebouncer;
    private readonly TimeSpan _bagSyncDelay = TimeSpan.FromMilliseconds(300);
    private bool _pendingBagSync;

    public WebViewNotificationThrottle(WebViewHub webViewHub, TimeSpan? flushInterval = null)
    {
        _webViewHub = webViewHub ?? throw new ArgumentNullException(nameof(webViewHub));
        _flushInterval = flushInterval ?? TimeSpan.FromMilliseconds(200);

        // 定期批次處理通知
        _flushTimer = new System.Threading.Timer(_ => FlushNotifications(), null, _flushInterval, _flushInterval);
    }

    #region 物品拾取通知（批次處理）

    /// <summary>
    /// 通知物品拾取（批次處理）
    /// </summary>
    public void NotifyItemPicked(string itemName, int quantity)
    {
        _notificationQueue.Enqueue(new NotificationItem
        {
            Type = NotificationType.ItemPicked,
            ItemName = itemName,
            Quantity = quantity,
            Timestamp = DateTime.Now
        });
    }

    #endregion

    #region 當前地圖更新（防抖動）

    /// <summary>
    /// 通知當前地圖更新（防抖動：500ms 內只發送最後一次）
    /// </summary>
    public void NotifyCurrentMapUpdate(MapRecordViewModel mapData)
    {
        lock (_lock)
        {
            // 更新待發送的資料
            _pendingCurrentMapData = mapData;

            // 重置防抖動計時器
            _currentMapUpdateDebouncer?.Dispose();
            _currentMapUpdateDebouncer = new System.Threading.Timer(_ =>
            {
                FlushCurrentMapUpdate();
            }, null, _currentMapUpdateDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// 立即發送當前地圖更新（不等待防抖動）
    /// </summary>
    public async Task NotifyCurrentMapUpdateImmediateAsync(MapRecordViewModel mapData)
    {
        try
        {
            await _webViewHub.NotifyCurrentMapUpdateAsync(mapData);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "立即發送當前地圖更新失敗");
        }
    }

    private void FlushCurrentMapUpdate()
    {
        MapRecordViewModel dataToSend;
        lock (_lock)
        {
            if (_pendingCurrentMapData == null)
                return;

            dataToSend = _pendingCurrentMapData;
            _pendingCurrentMapData = null;
        }

        // 非同步發送，不阻塞
        _ = Task.Run(async () =>
        {
            try
            {
                await _webViewHub.NotifyCurrentMapUpdateAsync(dataToSend);
                Log.Debug("批次發送當前地圖更新");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "發送當前地圖更新失敗");
            }
        });
    }

    #endregion

    #region 🆕 背包同步通知（防抖動）

    /// <summary>
    /// 通知背包同步完成（防抖動：300ms 內只發送最後一次）
    /// 用於背包初始化或大量物品變更時避免頻繁通知
    /// </summary>
    public void NotifyBagSync()
    {
        lock (_lock)
        {
            // 標記有待發送的背包同步
            _pendingBagSync = true;

            // 重置防抖動計時器
            _bagSyncDebouncer?.Dispose();
            _bagSyncDebouncer = new System.Threading.Timer(_ =>
            {
                FlushBagSync();
            }, null, _bagSyncDelay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// 立即發送背包同步通知（不等待防抖動）
    /// </summary>
    public async Task NotifyBagSyncImmediateAsync()
    {
        try
        {
            await _webViewHub.NotifyBagSyncAsync();
            Log.Debug("立即發送背包同步通知");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "立即發送背包同步通知失敗");
        }
    }

    private void FlushBagSync()
    {
        bool shouldSend;
        lock (_lock)
        {
            shouldSend = _pendingBagSync;
            _pendingBagSync = false;
        }

        if (!shouldSend)
            return;

        // 非同步發送，不阻塞
        _ = Task.Run(async () =>
        {
            try
            {
                await _webViewHub.NotifyBagSyncAsync();
                Log.Debug("批次發送背包同步通知");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "發送背包同步通知失敗");
            }
        });
    }

    #endregion

    #region 新地圖記錄通知（立即發送）

    /// <summary>
    /// 通知新地圖記錄（立即發送，不節流）
    /// </summary>
    public async Task NotifyNewMapRecordAsync()
    {
        try
        {
            await _webViewHub.NotifyNewMapRecordAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "發送新地圖記錄通知失敗");
        }
    }

    #endregion

    #region 批次處理

    /// <summary>
    /// 批次處理所有待發送的通知
    /// </summary>
    private void FlushNotifications()
    {
        if (_disposed || _notificationQueue.IsEmpty)
            return;

        try
        {
            // 收集所有待處理的物品拾取通知
            var itemPickedNotifications = new List<NotificationItem>();

            while (_notificationQueue.TryDequeue(out var notification))
            {
                if (notification.Type == NotificationType.ItemPicked)
                {
                    itemPickedNotifications.Add(notification);
                }
            }

            // 批次發送物品拾取通知
            if (itemPickedNotifications.Count > 0)
            {
                FlushItemPickedNotifications(itemPickedNotifications);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批次處理通知時發生錯誤");
        }
    }

    /// <summary>
    /// 批次發送物品拾取通知（一次發送所有物品）
    /// </summary>
    private void FlushItemPickedNotifications(List<NotificationItem> notifications)
    {
        // 合併相同物品的數量
        var mergedItems = notifications
            .GroupBy(n => n.ItemName)
            .Select(g => new
            {
                ItemName = g.Key,
                TotalQuantity = g.Sum(n => n.Quantity),
                Count = g.Count()
            })
            .ToList();

        // 🆕 一次性發送所有物品（而非逐個發送）
        _ = Task.Run(async () =>
        {
            try
            {
                // 🔥 改為一次發送所有物品的批次資料
                await _webViewHub.NotifyItemsPickedBatchAsync(
                    mergedItems.Select(item => new
                    {
                        item.ItemName,
                        Quantity = item.TotalQuantity
                    }).ToArray()
                );

                Log.Debug("批次發送物品拾取通知：{Count} 筆原始通知合併為 {MergedCount} 種物品，一次發送完成",
                    notifications.Count, mergedItems.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "批次發送物品拾取通知失敗");
            }
        });
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // 最後一次批次處理
        FlushNotifications();
        FlushCurrentMapUpdate();
        FlushBagSync(); // 🆕 最後一次發送背包同步

        _flushTimer?.Dispose();
        _currentMapUpdateDebouncer?.Dispose();
        _bagSyncDebouncer?.Dispose(); // 🆕 釋放背包同步計時器
    }

    #endregion

    #region 內部類型

    private class NotificationItem
    {
        public NotificationType Type { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private enum NotificationType
    {
        ItemPicked,
        CurrentMapUpdate,
        NewMapRecord,
        BagSync // 🆕 背包同步類型
    }

    #endregion
}
