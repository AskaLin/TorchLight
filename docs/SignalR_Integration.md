# SignalR 整合與功能擴展說明

## 實作功能

### 1. WebView2 雙向通訊機制
- ? 創建 `WebViewHub` 服務，使用 WebView2 的 `postMessage` 實現 C# 到前端的即時通訊
- ? 前端使用 `window.addEventListener('message')` 接收後端訊息
- ? 不需要額外安裝 SignalR 伺服器，直接使用 WebView2 內建功能

### 2. 監聽 "已開啟日誌" 事件
- ? 在 `LineParser` 新增 `IsLogOpenedMessage()` 方法
- ? 檢測遊戲日誌中的 `MsgMgr@:Show MsgValue = 已開啟日誌` 訊息
- ? 在 `GameLogProcessor` 中處理該事件並觸發 `OnLogOpenedDetected` 事件
- ? `MainWindow` 接收事件並通知前端更新狀態為「監控日誌中」

### 3. 前端狀態管理
- ? 在 `mapStore.js` 新增：
  - `monitoringStatus`: 監控狀態（待機中 / 監控日誌中）
  - `lastBagSyncTime`: 背包上次同步時間
  - `handleBackendMessage()`: 處理來自後端的即時訊息
- ? 在 `Home.vue` 顯示：
  - 監控狀態（帶顏色指示）
  - 背包上次同步時間（格式化顯示）

### 4. 背包同步狀態追蹤
- ? 在 `GameLogProcessor` 新增 `OnBagSyncCompleted` 事件
- ? 背包初始化完成時自動觸發事件
- ? 前端即時更新背包同步時間

## 技術架構

```
遊戲日誌 → GameLogProcessor → MainWindow → WebViewHub → postMessage → 前端 Vue Store
                ↓          ↓
          事件觸發            狀態更新
```

## 訊息格式

### 後端發送格式
```json
{
  "type": "logMonitoringStatus" | "bagSyncStatus" | "newMapRecord" | "itemPicked",
  "data": {
    // 依不同類型而異
  },
  "timestamp": "2025-01-30T12:34:56"
}
```

### 前端接收處理
- `logMonitoringStatus`: 更新監控狀態
- `bagSyncStatus`: 更新背包同步時間
- `newMapRecord`: 刷新地圖記錄列表
- `itemPicked`: 顯示即時拾取通知

## 檔案變更清單

### 後端 C#
1. **新增** `Services/WebViewHub.cs` - WebView2 通訊中樞
2. **修改** `LineParser.cs` - 新增 `IsLogOpenedMessage()` 方法
3. **修改** `GameLogProcessor.cs` - 新增事件觸發機制
4. **修改** `UI/MainWindow.cs` - 整合 WebViewHub 並註冊事件

### 前端 Vue
1. **修改** `stores/mapStore.js` - 新增狀態與訊息監聽
2. **修改** `views/Home.vue` - 顯示監控狀態與同步時間

## 使用範例

### 後端發送訊息
```csharp
await _webViewHub.NotifyLogMonitoringStatusAsync("監控日誌中");
await _webViewHub.NotifyBagSyncStatusAsync(DateTime.Now);
```

### 前端接收訊息
```javascript
// 自動監聽，無需手動設定
// mapStore 會自動更新 monitoringStatus 和 lastBagSyncTime
```

## 狀態顏色指示

- ?? **監控日誌中** - 綠色（#4caf50）
- ?? **進行中（地圖內）** - 橙色（#ff9800）
- ? **待機中** - 灰色（半透明）

## 測試驗證

1. ? 編譯成功
2. ? 執行測試（需要實際遊戲日誌）
3. ? 確認前端接收訊息正常
4. ? 驗證狀態更新即時性

## 後續擴展建議

1. 新增更多即時通知類型（如：稀有物品拾取、地圖完成等）
2. 實作前端到後端的訊息傳遞（如：手動觸發同步）
3. 新增錯誤重連機制
4. 實作訊息佇列處理
