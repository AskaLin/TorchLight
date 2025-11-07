# WebView 通知頻率優化方案

## 📊 問題分析

### 原有問題
`GameLogProcessor` 中 `_webViewHub` 的呼叫頻率過高，主要場景：

1. **物品拾取時** - 每撿一個物品呼叫 2 次：
   ```csharp
   await _webViewHub.NotifyItemPickedAsync(itemName, quantity);
   await _webViewHub.NotifyCurrentMapUpdateAsync(mapData);
   ```

2. **地圖狀態變更** - 頻繁更新當前地圖資訊

3. **背包同步** - 初始化時可能觸發多次更新

### 性能影響
- 前端頻繁重繪 UI
- 網路通訊開銷大
- 可能造成 UI 卡頓
- 瀏覽器 JavaScript 執行壓力大

---

## 💡 解決方案：通知節流器

### 核心概念

**WebViewNotificationThrottle** - 智能通知管理器

#### 1️⃣ **批次處理**（物品拾取通知）
```
拾取物品 A (x3) ──┐
拾取物品 B (x5) ──┼─► 收集 200ms ──► 合併發送 ──► A: 10, B: 5
拾取物品 A (x7) ──┘
```

**優點：**
- 相同物品數量自動合併
- 減少通訊次數（N 次 → 1 次）
- 前端只更新一次

#### 2️⃣ **防抖動**（地圖更新通知）
```
更新 1 ──┐
更新 2 ──┼─► 等待 500ms 內無新更新 ──► 發送最後一次更新
更新 3 ──┘
```

**優點：**
- 避免頻繁更新同一資料
- 只發送最新狀態
- 大幅減少通訊次數

#### 3️⃣ **立即發送**（重要事件）
```
新地圖記錄 ──► 立即發送（不延遲）
```

**優點：**
- 重要事件不延遲
- 用戶體驗不受影響

---

## 🔧 使用方式

### 1. 初始化節流器

```csharp
public GameLogProcessor(WebViewHub webViewHub = null)
{
    _webViewHub = webViewHub;
    
    // 初始化通知節流器
    if (_webViewHub != null)
    {
        _notificationThrottle = new WebViewNotificationThrottle(_webViewHub);
    }
    // ...
}
```

### 2. 物品拾取（批次處理）

**原有程式碼（高頻）：**
```csharp
// ❌ 每次拾取都立即發送 2 次通知
await _webViewHub.NotifyItemPickedAsync(itemName, quantity);
await _webViewHub.NotifyCurrentMapUpdateAsync(mapData);
```

**改進後（批次）：**
```csharp
// ✅ 加入佇列，200ms 後批次發送
_notificationThrottle.NotifyItemPicked(itemName, quantity);
_notificationThrottle.NotifyCurrentMapUpdate(mapData);
```

### 3. 地圖更新（防抖動）

**原有程式碼（高頻）：**
```csharp
// ❌ 每次狀態變更都立即發送
await _webViewHub.NotifyCurrentMapUpdateAsync(GetCurrentMapData());
```

**改進後（防抖動）：**
```csharp
// ✅ 500ms 內只發送最後一次
_notificationThrottle.NotifyCurrentMapUpdate(GetCurrentMapData());
```

### 4. 新地圖記錄（立即發送）

```csharp
// ✅ 重要事件立即發送，不延遲
await _notificationThrottle.NotifyNewMapRecordAsync();
```

---

## 📈 效能提升

### 場景 1: 快速拾取 10 個物品

| 方案 | 通知次數 | 說明 |
|------|---------|------|
| **原有** | 20 次 | 每個物品 2 次（拾取 + 地圖更新） |
| **改進後** | 2-3 次 | 批次合併 + 防抖動 |
| **減少** | 85-90% | 🎉 |

### 場景 2: 地圖狀態頻繁變更

| 方案 | 通知次數 | 說明 |
|------|---------|------|
| **原有** | N 次 | 每次變更都發送 |
| **改進後** | 1 次 | 500ms 內只發送最後一次 |
| **減少** | (N-1)/N | 🎉 |

---

## ⚙️ 可調參數

### 批次處理間隔
```csharp
// 預設 200ms，可調整
new WebViewNotificationThrottle(_webViewHub, flushInterval: TimeSpan.FromMilliseconds(300));
```

### 防抖動延遲
```csharp
// 在 WebViewNotificationThrottle.cs 中修改
private readonly TimeSpan _currentMapUpdateDelay = TimeSpan.FromMilliseconds(500);
```

---

## 🎯 最佳實踐

### ✅ 適合使用節流器的場景
- 物品拾取通知（高頻、可合併）
- 地圖狀態更新（頻繁變更、只需最新狀態）
- 背包同步狀態（多次更新、只需最終狀態）

### ❌ 不適合使用節流器的場景
- 新地圖記錄（重要事件，需立即通知）
- 錯誤訊息（需即時反饋）
- 用戶操作回應（需即時回饋）

---

## 🔍 監控與調試

### 啟用調試日誌
```csharp
Log.Debug("批次發送 {Count} 個物品拾取通知（合併為 {MergedCount} 種物品）",
    notifications.Count, mergedItems.Count);
```

### 範例輸出
```
[DEBUG] 批次發送 15 個物品拾取通知（合併為 3 種物品）
  - 神聖石: 10
  - 混沌石: 3
  - 鏡子碎片: 2
```

---

## 🚀 其他優化方案（備選）

### 方案 2: SignalR（適合多人遊戲或實時性要求極高的場景）
```csharp
// 使用 SignalR Hub 替代 WebView2 通訊
public class GameHub : Hub
{
    public async Task NotifyItemPicked(string itemName, int quantity)
    {
        await Clients.All.SendAsync("ItemPicked", itemName, quantity);
    }
}
```

**優點：**
- 雙向實時通訊
- 支援多客戶端
- 自動重連

**缺點：**
- 需要額外的伺服器
- 增加架構複雜度

### 方案 3: 事件匯流排（適合複雜事件處理）
```csharp
public class EventBus
{
    private readonly Channel<IEvent> _channel = Channel.CreateUnbounded<IEvent>();
    
    public async Task PublishAsync(IEvent ev)
    {
        await _channel.Writer.WriteAsync(ev);
    }
}
```

**優點：**
- 完全解耦
- 支援訂閱模式
- 易於擴展

**缺點：**
- 增加程式碼複雜度
- 需要設計事件體系

---

## 📝 總結

### 改進成果
- ✅ **減少 85-90% 通訊次數**
- ✅ **前端 UI 更新更流暢**
- ✅ **降低 CPU 與記憶體使用**
- ✅ **改善用戶體驗**

### 實作重點
1. **批次處理** - 合併相同類型的通知
2. **防抖動** - 只發送最新狀態
3. **優先級** - 重要事件立即發送

### 未來優化方向
- 支援可配置的延遲時間
- 增加通知優先級系統
- 加入通知統計與監控

---

**文件版本**: 1.0  
**最後更新**: 2025/01/XX  
**維護者**: TorchLight Statistics Team
