# Commit Summary

## 功能新增與優化

### 1. 地圖設定管理系統 (Map Settings Management)

#### 新增檔案
- `src/TorchLight.Statistics/Models/MapInfoConfig.cs` - 地圖設定資料模型
- `src/TorchLight.Statistics/mapInfo.json` - 地圖設定檔案
- `src/TorchLight.Statistics/wwwroot-src/src/views/MapSettings.vue` - 地圖設定管理頁面

#### 功能特性
- ✅ 支援從 JSON 檔案動態載入地圖設定
- ✅ 前端介面可新增、編輯、刪除地圖設定
- ✅ FileSystemWatcher 監控檔案變更，自動重新載入
- ✅ 錯誤處理：設定更新失敗時自動恢復備份
- ✅ 執行緒安全：使用 lock 保護共享資料

#### 修改檔案
- `src/TorchLight.Statistics/MapMapper.cs` - 重構為動態載入機制
- `src/TorchLight.Statistics/UI/WebViewApi.cs` - 新增地圖設定 API
- `src/TorchLight.Statistics/Services/WebViewHub.cs` - 新增設定更新通知
- `src/TorchLight.Statistics/UI/MainWindow.cs` - 註冊設定更新事件
- `src/TorchLight.Statistics/Program.cs` - 初始化 MapMapper
- `src/TorchLight.Statistics/wwwroot-src/src/router/index.js` - 新增設定頁路由
- `src/TorchLight.Statistics/wwwroot-src/src/components/Header.vue` - 新增設定選單

### 2. 當前地圖資訊顯示 (Current Map Info)

#### 新增檔案
- `src/TorchLight.Statistics/wwwroot-src/src/components/CurrentMapInfo.vue` - 當前地圖資訊元件

#### 功能特性
- ✅ 即時顯示當前所在地圖
- ✅ 顯示地圖類型（藏身處/異界）
- ✅ 異界地圖顯示拾取物品統計
- ✅ 支援展開/收合物品列表

#### 修改檔案
- `src/TorchLight.Statistics/wwwroot-src/src/views/Home.vue` - 整合當前地圖元件
- `src/TorchLight.Statistics/wwwroot-src/src/stores/mapStore.js` - 新增當前地圖狀態管理

### 3. 前端介面優化 (Frontend UI Optimization)

#### 第一階段優化
- ✅ 地圖詳情：改為網格佈局，每行 4 列
- ✅ 地圖詳情：移除標題和欄位分布資訊
- ✅ 地圖詳情：只顯示物品總數量
- ✅ 首頁：總地圖數卡片可點擊跳轉

#### 第二階段優化
- ✅ 地圖詳情：物品名稱不換行，超出顯示省略號
- ✅ 地圖詳情：數量不換行
- ✅ 統計頁面：總地圖數可點擊跳轉
- ✅ 統計頁面：Top 10 改為兩列佈局（1-5 左列，6-10 右列）

#### 修改檔案
- `src/TorchLight.Statistics/wwwroot-src/src/views/MapDetail.vue`
- `src/TorchLight.Statistics/wwwroot-src/src/views/Statistics.vue`
- `src/TorchLight.Statistics/wwwroot-src/src/views/Home.vue`

### 4. 地圖設定同步修復 (Map Config Sync Fix)

#### 問題
- 前端修改地圖設定後，地圖記錄列表不同步更新

#### 解決方案
- ✅ 前端：添加 `mapConfigUpdated` 訊息處理，自動重新載入
- ✅ 後端：API 即時從 MapMapper 查詢最新地圖名稱

#### 修改檔案
- `src/TorchLight.Statistics/wwwroot-src/src/stores/mapStore.js`
- `src/TorchLight.Statistics/UI/WebViewApi.cs` - 修改 GetMapRecords, GetMapRecordDetail, GetCurrentMapInfo

### 5. 其他改進

#### Emoji 修正
- `src/TorchLight.Statistics/wwwroot-src/src/components/Header.vue` - 修正 logo emoji 編碼

#### 專案配置
- `src/TorchLight.Statistics/TorchLight.Statistics.csproj` - 新增 mapInfo.json 輸出設定

#### 背包同步優化
- `src/TorchLight.Statistics/GameLogProcessor.cs` - 改進背包初始化判斷邏輯
- `src/TorchLight.Statistics/Services/MapPickRecordManager.cs` - 優化記錄管理

## 文檔新增

- `docs/MAP_SETTINGS_FEATURE.md` - 地圖設定功能完整說明
- `docs/MAP_INFO_FILE_LOCATION.md` - mapInfo.json 位置說明
- `docs/CURRENT_MAP_INFO_FEATURE.md` - 當前地圖資訊功能說明
- `docs/FRONTEND_OPTIMIZATION.md` - 前端優化第一階段
- `docs/FRONTEND_OPTIMIZATION_PHASE2.md` - 前端優化第二階段
- `docs/MAP_CONFIG_SYNC_FIX.md` - 地圖設定同步修復說明
- `docs/EMOJI_FIX.md` - Emoji 編碼問題修復

## 技術特點

### 架構改進
- 從靜態資料改為動態配置
- 支援檔案監控和熱重載
- 前後端即時通訊機制

### 程式碼品質
- 執行緒安全（lock 保護）
- 錯誤處理和恢復機制
- 防抖動機制（debounce）
- 響應式設計

### 使用者體驗
- 無需重啟應用程式即可更新設定
- 前端自動響應後端變更
- 清晰的視覺回饋
- 流暢的操作體驗

## 變更統計

### 新增檔案
- 2 個 C# 模型/配置檔案
- 2 個 Vue 元件
- 7 個文檔檔案

### 修改檔案
- 6 個後端 C# 檔案
- 7 個前端 Vue/JS 檔案
- 1 個專案配置檔案

### 刪除檔案
- `src/TorchLight.Statistics/IdTable.conf` - 已不再使用

## 測試建議

- [ ] 測試地圖設定新增/編輯/刪除功能
- [ ] 測試檔案監控和自動重新載入
- [ ] 測試前端介面響應式佈局
- [ ] 測試地圖名稱即時同步
- [ ] 測試當前地圖資訊顯示
- [ ] 測試統計頁面 Top 10 顯示
- [ ] 測試各種錯誤情況的處理

## Breaking Changes

無重大破壞性變更。所有變更向下相容。

## 版本資訊

- .NET 版本：8.0
- 目標平台：Windows
- 主要套件：WebView2, Serilog, System.Text.Json
