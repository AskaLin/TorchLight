# 當前地圖資訊功能實現說明

## 功能概述
在前端新增「當前地圖資訊」顯示模組，根據地圖類型顯示不同的資訊：

### 1. 避難所地圖（Hideout）
- 只顯示地圖名稱
- 簡潔明瞭的呈現方式

### 2. 異界地圖（Netherrealm）
- 顯示完整地圖資訊
- 包含以下內容：
  - 地圖名稱
  - 進圖時間
  - 開圖材料（門票、羅盤、探針）
  - 拾取物品列表（一行四列顯示）

## 技術實現

### 後端修改

#### 1. GameLogProcessor.cs
- 新增 `WebViewHub` 依賴注入支援
- 新增 `SetWebViewHub()` 方法以便後續設定
- 新增 `GetCurrentMapData()` 方法來準備當前地圖資料
- 在地圖切換時通知前端更新
- 在物品拾取時通知前端更新

#### 2. MapPickRecordManager.cs
- 新增 `GetCurrentMapRecord()` 方法
- 返回包含當前拾取記錄的地圖資料副本

#### 3. WebViewHub.cs
- 新增 `NotifyCurrentMapUpdateAsync()` 方法
- 支援發送當前地圖資訊到前端

#### 4. WebViewApi.cs
- 更新 `GetCurrentMapInfo()` 方法
- 根據地圖類型返回不同結構的資料
- 避難所：只返回基本資訊
- 異界：返回完整資訊包含拾取物品

#### 5. MainWindow.cs
- 修改構造函數以接受 `WebViewHub` 參數
- 初始化時將 `WebViewHub` 傳遞給 `GameLogProcessor`

#### 6. Program.cs
- 創建 `WebViewHub` 實例
- 在創建 `GameLogProcessor` 後設定 `WebViewHub`
- 將 `WebViewHub` 傳遞給 `MainWindow`

### 前端修改

#### 1. mapStore.js
- 新增處理 `currentMapUpdate` 訊息的邏輯
- 在 `itemPicked` 事件時更新當前地圖資訊
- 更新 `currentMapInfo` 狀態

#### 2. CurrentMapInfo.vue（新建）
- 創建當前地圖資訊顯示組件
- 響應式設計，支援不同螢幕尺寸
- 區分避難所和異界地圖的顯示方式
- 拾取物品使用網格布局（一行四列）
- 美觀的視覺效果和過渡動畫

#### 3. Home.vue
- 導入並使用 `CurrentMapInfo` 組件
- 在歡迎卡片後顯示當前地圖資訊

## 資料流程

```
遊戲日誌變更
    ↓
GameLogProcessor 處理
    ↓
地圖切換/物品拾取
    ↓
WebViewHub.NotifyCurrentMapUpdateAsync()
    ↓
前端接收 'currentMapUpdate' 訊息
    ↓
更新 mapStore.currentMapInfo
    ↓
CurrentMapInfo 組件顯示更新
```

## UI 設計特點

### 避難所地圖卡片
- 簡單的標籤-值對顯示
- 半透明背景
- 白色文字

### 異界地圖卡片
- 地圖頭部：漸變紫色背景，顯示地圖名稱和進圖時間
- 開圖材料：圖標+標籤顯示，包含門票、羅盤、探針
- 拾取物品：
  - 網格布局（4列）
  - 每個物品卡片包含物品名稱和數量
  - 綠色主題，代表拾取/收穫
  - Hover 效果：卡片上升+陰影
  - 響應式設計：
    - 1200px 以下：3 列
    - 900px 以下：2 列
    - 600px 以下：1 列

### 無地圖狀態
- 顯示地球圖標和提示文字
- 虛線邊框
- 半透明背景

## 未來擴展建議

1. **物品篩選功能**
   - 可按物品類型篩選顯示
   - 可按價值排序

2. **統計資訊**
   - 顯示當前地圖的拾取物品總價值
   - 顯示稀有度分布

3. **歷史對比**
   - 與上一次相同地圖的拾取比較
   - 顯示最佳記錄

4. **即時通知**
   - 拾取稀有物品時的特效提示
   - 音效通知

## 測試建議

1. **避難所地圖測試**
   - 確認只顯示地圖名稱
   - 驗證無多餘資訊顯示

2. **異界地圖測試**
   - 確認所有資訊正確顯示
   - 驗證物品拾取即時更新
   - 測試不同數量物品的顯示（1-20+個）
 - 驗證響應式布局在不同螢幕尺寸下的表現

3. **地圖切換測試**
   - 從避難所到異界
   - 從異界到避難所
   - 異界之間切換
   - 驗證資料正確清除和更新

4. **邊界情況測試**
   - 無羅盤、無探針的情況
   - 無拾取物品的情況
   - 大量物品拾取的性能

## 注意事項

1. **Nullable 註釋**
   - GameLogProcessor 中使用 `#nullable enable/disable` 包裹事件定義
   - 避免 CS8632 編譯警告

2. **非同步操作**
   - 通知前端的操作使用 `Task.Run` 避免阻塞
 - 使用 `_ =` 忽略返回值（Fire and Forget 模式）

3. **資料一致性**
   - `GetCurrentMapRecord()` 返回副本，包含當前拾取記錄
   - 使用 `DateTime.Now` 作為臨時結束時間

4. **效能考量**
   - 前端使用 computed 屬性減少不必要的計算
   - LINQ 查詢優化，避免多次遍歷

## 檔案清單

### 新增檔案
- `src/TorchLight.Statistics/wwwroot-src/src/components/CurrentMapInfo.vue`

### 修改檔案
- `src/TorchLight.Statistics/GameLogProcessor.cs`
- `src/TorchLight.Statistics/Services/MapPickRecordManager.cs`
- `src/TorchLight.Statistics/Services/WebViewHub.cs`
- `src/TorchLight.Statistics/UI/WebViewApi.cs`
- `src/TorchLight.Statistics/UI/MainWindow.cs`
- `src/TorchLight.Statistics/Program.cs`
- `src/TorchLight.Statistics/wwwroot-src/src/stores/mapStore.js`
- `src/TorchLight.Statistics/wwwroot-src/src/views/Home.vue`
