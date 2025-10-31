# 歷史記錄功能調試指南

## 問題現象
點擊「歷史紀錄」→「查看詳細紀錄」後，顯示「找不到歷史記錄資料」。

## 調試步驟

### 1. 檢查數據流程

打開瀏覽器開發者工具（F12），查看 Console 輸出：

#### HistoryRecord.vue 的日誌：
```
🔍 Requesting history detail for file: TorchPickRecord_1224_1430.json
📦 Received detail response: { summary: {...}, records: [...] }
✅ History detail loaded successfully
  - Total maps: 10
  - Records count: 10
```

#### HistoryDetail.vue 的日誌：
```
Loaded history data from router state: {...}
```
或
```
Loading history data from backend for file: TorchPickRecord_1224_1430.json
Loaded history data from backend: {...}
```

### 2. 檢查後端日誌

查看應用程式輸出（Console），應該看到：

```
[INF] 📂 開始載入歷史記錄: TorchPickRecord_1224_1430.json
[DBG]   - 存檔目錄: E:\SideProjects\TorchLight\bin\Debug\net8.0\Saved
[DBG]   - 完整路徑: E:\SideProjects\TorchLight\bin\Debug\net8.0\Saved\TorchPickRecord_1224_1430.json
[INF] ✅ 成功載入歷史記錄
[DBG]   - 總地圖數: 10
[DBG]   - 記錄數量: 10
```

### 3. 檢查檔案是否存在

手動檢查 `Saved` 資料夾：

**位置：** 應用程式執行目錄下的 `Saved` 資料夾

**開發模式路徑範例：**
```
E:\SideProjects\TorchLight\bin\Debug\net8.0\Saved\
```

**檢查項目：**
1. 資料夾是否存在？
2. 是否有 `.json` 檔案？
3. 檔案名稱格式是否正確？（`TorchPickRecord_MMdd_HHmm.json`）
4. 檔案大小是否合理？（不是 0 KB）

### 4. 檢查檔案內容格式

打開其中一個 JSON 檔案，檢查格式是否正確：

```json
{
  "summary": {
    "totalMaps": 10,
    "totalItems": 50,
    "totalQuantity": 5000,
    "totalPlayTime": "02:30:45",
    "mostPickedItems": [
      {
  "baseId": 100300,
        "name": "初火源質",
        "totalQuantity": 1000,
        "like": 6
      }
    ]
  },
  "records": [
    {
      "id": "GeBuLinCunLuo01",
      "name": "災厄之林",
      "recordId": "xxx",
      "startTime": "2024-12-24T14:30:00",
  "endTime": "2024-12-24T14:45:00",
      "useTime": "00:15:00",
      "pickRecord": {
        "100300": {
          "baseId": 100300,
        "name": "初火源質",
  "total": 100,
     "slots": {}
        }
      }
    }
  ],
  "savedTime": "2024-12-24T14:45:10"
}
```

### 5. 常見問題排查

#### 問題 1：Saved 資料夾不存在
**原因：** 尚未結束過任何地圖
**解決：** 進入遊戲刷一張圖並離開，觸發自動存檔

#### 問題 2：檔案格式錯誤
**檢查：**
```bash
# 確認檔案是否為有效 JSON
cat Saved/TorchPickRecord_1224_1430.json | jq .
```

**可能原因：**
- 序列化時發生錯誤
- 檔案寫入被中斷

**解決：** 刪除錯誤的檔案，重新刷圖生成新記錄

#### 問題 3：Router state 丟失
**現象：**
- Console 顯示從 router state 載入失敗
- 後端也沒有收到請求

**原因：** Vue Router 的 state 在某些情況下會丟失

**解決：** 已修正，現在會自動從後端重新載入

#### 問題 4：API 調用失敗
**檢查：** Console 是否有錯誤

```javascript
// 正常
Received detail response: {...}

// 錯誤
Failed to load history detail: Error: xxx
```

**解決：** 檢查 WebView2 橋接是否正常

### 6. 手動測試

#### 測試 1：從列表進入詳情
```
1. 進入「歷史紀錄」頁面
2. 查看是否有記錄列表
3. 點擊「查看詳細紀錄」
4. 觀察 Console 輸出
5. 檢查是否正常顯示詳情頁
```

#### 測試 2：直接進入詳情頁（URL）
```
手動在 URL 輸入：
#/history/detail?fileName=TorchPickRecord_1224_1430.json
```

這會測試「無 router state」的情況，頁面應該自動從後端載入數據。

#### 測試 3：檢查地圖記錄跳轉
```
1. 從歷史記錄詳情頁
2. 點擊某張地圖卡片
3. 檢查地圖詳情頁是否正常顯示
4. 點擊返回按鈕
5. 應該返回到歷史記錄的地圖列表
```

### 7. 數據流程圖

```
用戶點擊「查看詳細紀錄」
    ↓
HistoryRecord.vue:viewDetail()
    ↓
apiCall('GetHistoryRecordDetail', fileName)
    ↓
C# WebViewApi.GetHistoryRecordDetail(fileName)
    ├─ 檢查檔案是否存在
    ├─ 載入 JSON 檔案
    └─ 序列化返回
    ↓
JavaScript 收到 response
    ├─ 檢查是否有 error
    └─ router.push() 跳轉
    ↓
HistoryDetail.vue:onMounted()
    ├─ 優先檢查 route.state.historyData
    ├─ 如果沒有，用 route.query.fileName 重新載入
    └─ 顯示數據
```

### 8. 快速修復建議

如果以上都正常但還是顯示錯誤，嘗試以下方法：

#### 方法 1：清除瀏覽器緩存
```
F12 → Network → Disable cache
F5 重新整理
```

#### 方法 2：重新生成記錄
```
1. 刪除 Saved 資料夾中的所有檔案
2. 重新啟動應用程式
3. 進入遊戲刷圖
4. 等待自動保存
5. 再次測試歷史記錄功能
```

#### 方法 3：檢查 JSON 序列化設定
確保 `WebViewApi` 的序列化選項正確：

```csharp
private readonly JsonSerializerOptions _ops = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,  // ✅ 小駝峰命名
    WriteIndented = true  // ✅ 格式化輸出
};
```

### 9. 成功標誌

功能正常時應該看到：

#### 瀏覽器 Console：
```
✅ History detail loaded successfully
  - Total maps: 10
  - Records count: 10
Loaded history data from router state: {...}
```

#### 頁面顯示：
```
┌────────────────────────────────────┐
│ ← 返回歷史紀錄  📚 12/24 14:30的記錄│
├────────────────────────────────────┤
│ 統計摘要    │
│ 總地圖數: 10  物品種類: 50        │
│ ...          │
├────────────────────────────────────┤
│ 地圖記錄列表 │
│ [災厄之林] [長明宮城] ...         │
└────────────────────────────────────┘
```

### 10. 聯絡資訊

如果問題仍然存在，請提供：

1. 瀏覽器 Console 的完整輸出
2. 後端 Log 的完整輸出
3. Saved 資料夾的檔案清單
4. 其中一個 JSON 檔案的內容（前 50 行）

這將幫助快速定位問題！
