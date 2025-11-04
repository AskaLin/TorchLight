# 環境參數設定功能實作說明

## 概述

為應用程式添加了環境參數設定功能，主要用於設定遊戲日誌檔案位置。此功能採用**純前端 HTML `<input type="file">`** 方式選擇檔案，並透過後端 API 驗證檔案有效性。

## 功能特性

### 1. 環境參數設定頁面
- 使用 HTML `<input type="file">` 選擇日誌檔案（簡單直觀）
- 即時後端驗證檔案有效性
- 常見檔案位置提示
- 儲存設定功能
- 重置功能

### 2. 啟動時自動檢查
- 程式啟動時自動檢查環境設定
- 如果未設定或設定無效，顯示提示對話框
- 引導使用者前往設定頁面

### 3. 設定持久化
- 設定儲存在 `appsettings.json` 檔案中
- 使用 `AppSettingsManager` 管理設定讀寫

### 4. 智慧路徑解析
- 優先讀取 `appsettings.json` 中設定的路徑
- 如果未設定，從預設候選路徑中搜尋
- 支援 Steam/Epic/自訂安裝位置

## 實作細節

### 後端實作

#### 1. 資料模型更新
**檔案**: `src\TorchLight.Statistics\Models\AppSettings.cs`

添加了 `EnvironmentSettings` 類別：

```csharp
/// <summary>
/// 環境參數設定
/// </summary>
public class EnvironmentSettings
{
    /// <summary>
    /// 遊戲日誌檔案位置（完整路徑含檔名）
    /// </summary>
    public string GameLogPath { get; set; } = string.Empty;
}
```

在 `AppSettings` 中添加：

```csharp
public class AppSettings
{
    public ExecuteLineSettings ExecuteLine { get; set; } = new();
    public EnvironmentSettings Environment { get; set; } = new();
}
```

#### 2. Program.cs 路徑讀取邏輯
**檔案**: `src\TorchLight.Statistics\Program.cs`

更新了 `GetLogFilePath()` 方法：

```csharp
static string GetLogFilePath()
{
    // 1. 優先從 appsettings.json 讀取使用者設定的路徑
    var settings = Services.AppSettingsManager.GetSettings();
    var configuredPath = settings?.Environment?.GameLogPath;

    if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
    {
        Log.Information("使用設定檔中的日誌路徑");
        return configuredPath;
    }

    // 2. 如果設定檔中沒有或路徑無效，嘗試從預設候選路徑中尋找
    Log.Information("設定檔中未設定日誌路徑或路徑無效，嘗試從預設路徑搜尋...");
    foreach (var path in AppConfiguration.CandidateLogPaths)
    {
        if (File.Exists(path))
        {
            Log.Information("在預設路徑中找到日誌檔案: {Path}", path);
            return path;
        }
    }

    // 3. 如果都找不到，返回設定檔中的路徑或預設路徑
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        Log.Warning("使用設定檔中的路徑（檔案不存在）: {Path}", configuredPath);
        return configuredPath;
    }

    Log.Warning("無法找到日誌檔案，返回預設路徑");
    return AppConfiguration.CandidateLogPaths[0];
}
```

**路徑解析優先順序**：
1. ✅ `appsettings.json` 中設定的路徑（如果檔案存在）
2. ✅ `AppConfiguration.CandidateLogPaths` 候選路徑列表
3. ⚠️ 返回設定檔中的路徑（即使不存在）或第一個候選路徑

#### 3. WebViewApi 新增方法
**檔案**: `src\TorchLight.Statistics\UI\WebViewApi.cs`

添加了三個 API 方法：

```csharp
/// <summary>
/// 獲取環境參數設定
/// </summary>
public string GetEnvironmentSettings()

/// <summary>
/// 驗證遊戲日誌檔案路徑
/// </summary>
public string ValidateGameLogPath(string gameLogPath)

/// <summary>
/// 儲存環境參數設定
/// </summary>
public string SaveEnvironmentSettings(string gameLogPath)
```

**API 回傳格式**:

- `GetEnvironmentSettings()`:
```json
{
  "gameLogPath": "C:\\Games\\Torchlight\\TorchLight.log",
  "isConfigured": true
}
```

- `ValidateGameLogPath(path)`:
```json
{
  "success": true,
  "message": "檔案路徑有效"
}
```

**驗證項目**：
1. ✅ 路徑不為空
2. ✅ 檔案存在
3. ✅ 檔案副檔名為 `.log`
4. ✅ 檔案可讀（嘗試開啟檔案）

- `SaveEnvironmentSettings(path)`:
```json
{
  "success": true,
  "message": "環境參數已儲存"
}
```

### 前端實作

#### 1. 環境設定元件
**檔案**: `src\TorchLight.Statistics\wwwroot-src\src\components\EnvironmentSettings.vue`

**核心實作**：

```vue
<template>
  <!-- 檔案選擇（使用 HTML input） -->
  <div class="path-input-group">
    <input type="text" v-model="gameLogPath" readonly />
    <label class="file-select-btn">
      📄 選擇檔案
      <input type="file" accept=".log" @change="handleFileSelect" style="display: none;" />
    </label>
  </div>
</template>

<script setup>
// 處理檔案選擇
function handleFileSelect(event) {
  const file = event.target.files[0]
  if (!file) return

  // 取得檔案的完整路徑（WebView2 環境支援 file.path）
  const filePath = file.path || file.webkitRelativePath || file.name

  if (file.path) {
    gameLogPath.value = file.path
    // 驗證檔案路徑
    validatePath(file.path)
  } else {
    showMessage('無法取得檔案路徑，請使用桌面應用程式版本', 'error')
  }
}

// 驗證檔案路徑
async function validatePath(path) {
  const result = await window.csharpApi.ValidateGameLogPath(path)
  const data = JSON.parse(result)

  if (data.success) {
    isPathValid.value = true
    showMessage('已選擇日誌檔案', 'info')
  } else {
    isPathValid.value = false
    showMessage('❌ ' + (data.message || '檔案路徑無效'), 'error')
  }
}

// 儲存設定
async function saveSettings() {
  if (!gameLogPath.value || !isPathValid.value) {
    showMessage('請選擇有效的日誌檔案', 'error')
    return
  }

  const result = await window.csharpApi.SaveEnvironmentSettings(gameLogPath.value)
  const data = JSON.parse(result)

  if (data.success) {
    showMessage('✅ 設定已儲存成功！', 'success')
  } else {
    showMessage('❌ ' + (data.message || '儲存失敗'), 'error')
  }
}
</script>
```

**UI 特色**：
- ✅ 使用 `<input type="file" accept=".log">` 自動過濾 .log 檔案
- ✅ 隱藏原生檔案輸入，使用自訂樣式按鈕
- ✅ 選擇檔案後立即驗證
- ✅ 即時顯示驗證結果（✅/❌）

#### 2. Settings.vue 更新
**檔案**: `src\TorchLight.Statistics\wwwroot-src\src\views\Settings.vue`

添加了「環境設定」Tab（預設第一個）：

```vue
<div class="tabs">
  <button @click="activeTab = 'environment'">環境設定</button>
  <button @click="activeTab = 'maps'">地圖設定</button>
  <button @click="activeTab = 'statistics'">拾取物品設定</button>
  <button @click="activeTab = 'executeLine'">斬殺線</button>
</div>

<div v-show="activeTab === 'environment'">
  <EnvironmentSettings />
</div>
```

#### 3. App.vue 啟動檢查
**檔案**: `src\TorchLight.Statistics\wwwroot-src\src\App.vue`

添加了啟動時的環境檢查：

```vue
<script setup>
const showConfigPrompt = ref(false)

onMounted(async () => {
  await checkEnvironmentConfig()
  // ...
})

async function checkEnvironmentConfig() {
  const result = await window.csharpApi.GetEnvironmentSettings()
  const data = JSON.parse(result)

  if (!data.isConfigured) {
    showConfigPrompt.value = true
  }
}

function goToSettings() {
  showConfigPrompt.value = false
  router.push({ name: 'settings' })
}
</script>

<template>
  <!-- 設定提示模態框 -->
  <div v-if="showConfigPrompt" class="config-prompt-overlay">
    <div class="config-prompt-modal">
      <div class="prompt-icon">⚠️</div>
      <h2>需要進行初始設定</h2>
      <p>請先設定遊戲日誌檔案位置</p>
      <button @click="goToSettings">前往設定</button>
    </div>
  </div>
</template>
```

## 使用流程

### 1. 首次啟動
```
應用程式啟動
    ↓
載入 appsettings.json
    ↓
Program.GetLogFilePath() 嘗試讀取設定的路徑
    ↓
路徑無效 → 搜尋預設候選路徑
    ↓
仍找不到 → 顯示警告但繼續啟動
    ↓
前端檢查環境設定 (GetEnvironmentSettings)
    ↓
isConfigured: false → 顯示提示對話框
    ↓
使用者點擊「前往設定」
    ↓
導向設定頁面（環境設定 Tab）
```

### 2. 設定流程（簡化版）
```
環境設定頁面
    ↓
點擊「選擇檔案」按鈕
    ↓
瀏覽器原生檔案選擇器打開
  - 自動過濾 *.log 檔案
    ↓
選擇遊戲日誌檔案 (TorchLight.log)
    ↓
前端取得檔案路徑 (file.path)
    ↓
呼叫後端驗證 ValidateGameLogPath(path)
  ↓ 驗證項目：
    1. 路徑不為空 ✅
    2. 檔案存在 ✅
    3. 副檔名 .log ✅
    4. 檔案可讀 ✅
    ↓
驗證成功 → 顯示「✅ 檔案路徑有效」
    ↓
點擊「儲存設定」
    ↓
呼叫 SaveEnvironmentSettings API
  → 再次驗證
  → 儲存到 appsettings.json
    ↓
顯示「✅ 設定已儲存成功！」
    ↓
下次啟動時 Program.GetLogFilePath() 會優先使用此路徑
```

### 3. 後續啟動
```
應用程式啟動
    ↓
Program.GetLogFilePath()
  → 讀取 appsettings.json 中的 GameLogPath
  → 檔案存在 ✅ → 使用此路徑
    ↓
前端檢查環境設定 (GetEnvironmentSettings)
    ↓
isConfigured: true → 正常啟動，不顯示提示
```

## 檔案結構

### 後端
```
src\TorchLight.Statistics\
├── Models\
│   └── AppSettings.cs                    (✨ 更新)
├── UI\
│   └── WebViewApi.cs                      (✨ 更新 - 簡化 API)
├── Program.cs                             (✨ 更新 - 修改 GetLogFilePath)
└── Services\
    └── AppSettingsManager.cs              (已存在)
```

### 前端
```
src\TorchLight.Statistics\wwwroot-src\src\
├── components\
│   └── EnvironmentSettings.vue           (✨ 更新 - 使用 input[type=file])
├── views\
│   └── Settings.vue                       (✨ 更新)
└── App.vue                                 (✨ 更新)
```

## 設定檔案格式

**appsettings.json**:
```json
{
  "executeLine": {
    "stage1Percentage": 20,
    "stage1Color": "#FF0000",
    ...
  },
  "environment": {
    "gameLogPath": "C:\\Games\\TorchLight\\Saved\\Logs\\TorchLight.log"
  }
}
```

## 常見檔案位置參考

### Steam 版本
```
C:\Program Files (x86)\Steam\steamapps\common\Torchlight Infinite\TorchLight\Saved\Logs\TorchLight.log
```

### Epic Games 版本
```
C:\Program Files\Epic Games\Torchlight Infinite\TorchLight\Saved\Logs\TorchLight.log
```

### 檔案名稱
通常為：`TorchLight.log`

## 錯誤處理

### 後端驗證（ValidateGameLogPath）
```csharp
// 1. 路徑空值檢查
if (string.IsNullOrWhiteSpace(gameLogPath))
    return "遊戲日誌檔案路徑不能為空";

// 2. 檔案存在性檢查
if (!File.Exists(gameLogPath))
    return "指定的日誌檔案不存在";

// 3. 檔案副檔名檢查
if (!gameLogPath.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
    return "請選擇有效的日誌檔案（.log）";

// 4. 檔案可讀性檢查
try {
    using var fs = new FileStream(gameLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
} catch {
    return "無法讀取日誌檔案，請確認檔案權限";
}
```

### 前端錯誤提示
- **空路徑**: 「請先選擇遊戲日誌檔案」
- **無效路徑**: 「❌ 檔案路徑無效或不存在」
- **無法讀取**: 「❌ 無法讀取日誌檔案，請確認檔案權限」
- **儲存失敗**: 顯示具體錯誤訊息

### Program.cs 路徑解析日誌
```
[INFO] 使用設定檔中的日誌路徑
[INFO] 設定檔中未設定日誌路徑或路徑無效，嘗試從預設路徑搜尋...
[INFO] 在預設路徑中找到日誌檔案: {Path}
[WARN] 使用設定檔中的路徑（檔案不存在）: {Path}
[WARN] 無法找到日誌檔案，返回預設路徑
```

## 設計優勢

### 為什麼選擇 HTML `<input type="file">`？

#### ✅ 優勢
1. **實作簡單**: 不需要 Windows Forms 對話框
2. **跨平台**: Web 標準，未來可能支援其他平台
3. **原生體驗**: 使用瀏覽器/OS 原生檔案選擇器
4. **自動過濾**: `accept=".log"` 自動過濾檔案類型
5. **無需額外 API**: 不需要 `OpenFileDialog` 等方法

#### ⚠️ 限制
- 需要 WebView2 環境支援 `file.path` 屬性
- 純 Web 瀏覽器環境無法取得完整路徑（安全限制）

### 架構對比

| 特性 | 原方案（對話框） | 新方案（HTML input） |
|------|-----------------|---------------------|
| 實作方式 | C# OpenFileDialog | HTML `<input type="file">` |
| 平台依賴 | Windows Forms | Web 標準 |
| 程式碼量 | 多（需要對話框處理） | 少（純 HTML） |
| 使用者體驗 | 原生 Windows 對話框 | 瀏覽器原生選擇器 |
| 跨平台 | ❌ Windows Only | ✅ 理論上可跨平台 |
| 自動過濾 | 手動設定 Filter | `accept=".log"` |

### 驗證流程對比

#### 原方案
```
選擇檔案（對話框）
    ↓
取得路徑
    ↓
送後端驗證（儲存時）
```

#### 新方案
```
選擇檔案（HTML input）
    ↓
取得路徑（file.path）
    ↓
立即送後端驗證 ✅
    ↓
儲存時再次驗證 ✅
```

**改進**：
- ✅ 選擇後立即驗證（更早發現問題）
- ✅ 儲存前再次驗證（雙重保險）
- ✅ 即時反饋（使用者體驗更好）

## 測試場景

### 1. 首次啟動測試
- [ ] 應顯示設定提示對話框
- [ ] 點擊「前往設定」應導向設定頁面
- [ ] 環境設定 Tab 應為預設選中

### 2. 檔案選擇測試
- [ ] 點擊「選擇檔案」按鈕應開啟檔案選擇器
- [ ] 檔案選擇器應過濾 *.log 檔案
- [ ] 選擇檔案後應立即驗證
- [ ] 有效檔案應顯示「✅ 檔案路徑有效」
- [ ] 無效檔案應顯示「❌ 檔案路徑無效」

### 3. 檔案驗證測試
- [ ] 選擇不存在的檔案 → 顯示「檔案不存在」
- [ ] 選擇非 .log 檔案 → 顯示「請選擇有效的日誌檔案」
- [ ] 選擇無法讀取的檔案 → 顯示「無法讀取，請確認權限」
- [ ] 選擇有效檔案 → 顯示「✅ 檔案路徑有效」

### 4. 儲存設定測試
- [ ] 未選擇檔案時「儲存設定」按鈕應停用
- [ ] 選擇無效檔案應無法儲存
- [ ] 儲存成功應顯示成功訊息
- [ ] 設定應正確寫入 appsettings.json
- [ ] 重新啟動應使用設定的路徑
- [ ] 不再顯示提示對話框

### 5. 路徑解析測試
- [ ] appsettings.json 中有效路徑 → 使用此路徑
- [ ] appsettings.json 中無效路徑 → 搜尋預設候選路徑
- [ ] 都找不到 → 返回預設路徑並顯示警告

### 6. 重置功能測試
- [ ] 點擊重置應清空所有輸入
- [ ] 重置後應可重新選擇檔案

## UI 特色

### 視覺設計
- **漸層背景**: 半透明背景層疊
- **動畫效果**: 訊息提示滑入動畫
- **狀態圖示**: ✅/❌ 即時狀態反饋
- **響應式設計**: 適應不同視窗大小

### 互動體驗
- **即時驗證**: 選擇檔案後立即驗證 ✨
- **雙重驗證**: 儲存時再次驗證 ✨
- **自動消失訊息**: 3 秒後自動清除
- **載入狀態**: 儲存時顯示「儲存中...」
- **禁用狀態**: 無效檔案時禁用儲存按鈕
- **檔案過濾**: `accept=".log"` 自動過濾

## 擴展性

### 未來可能的擴展
1. **拖放支援**: 支援拖放檔案到輸入框
2. **自動偵測**: 自動搜尋常見安裝位置的日誌檔案
3. **路徑歷史**: 記錄最近使用的檔案路徑
4. **進階設定**: 
   - 日誌監控頻率
   - 自動備份設定
   - 檔案過濾規則
   - 檔案輪替處理

## 總結

本次實作完成了：

✅ **簡化的檔案選擇**: 使用 HTML `<input type="file">` 取代對話框
✅ **後端 API**: 3 個環境設定相關的 API 方法
  - `GetEnvironmentSettings` - 獲取設定
  - `ValidateGameLogPath` - 🆕 驗證檔案路徑
  - `SaveEnvironmentSettings` - 儲存設定

✅ **路徑解析邏輯**: Program.GetLogFilePath() 智慧路徑解析
  1. 優先使用 appsettings.json 設定
  2. 搜尋預設候選路徑
  3. 返回預設路徑並記錄警告

✅ **前端元件**: 完整的環境設定 UI 元件
  - 使用 HTML `<input type="file">` （更簡單）
  - 即時驗證檔案路徑
  - 常見位置提示

✅ **設定管理**: 使用 AppSettingsManager 持久化設定

✅ **啟動檢查**: 自動檢查並引導使用者設定

✅ **使用者體驗**: 清晰的提示、即時驗證和錯誤處理

這個實作方式更簡單、更符合 Web 標準，並且提供了完整的檔案驗證機制。🎉
