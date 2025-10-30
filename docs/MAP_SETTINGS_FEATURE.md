# 地圖設定管理功能說明

## 功能概述

新增地圖設定管理功能，允許使用者透過前端介面新增、編輯、刪除地圖設定，設定資料儲存於專案根目錄的 `mapInfo.json` 檔案。

## 功能特色

### 1. 動態設定管理
- ✅ 從 JSON 檔案讀取地圖設定
- ✅ 支援新增、編輯、刪除地圖
- ✅ 自動儲存到 `mapInfo.json`
- ✅ 檔案變更時自動重新載入

### 2. 錯誤處理機制
- ✅ 檔案載入失敗時使用預設設定
- ✅ 檔案更新發生錯誤時維持原資料不變
- ✅ 前端即時顯示錯誤訊息

### 3. 即時同步
- ✅ 使用 FileSystemWatcher 監控檔案變更
- ✅ 檔案更新後自動通知前端
- ✅ 前端自動重新載入設定

## 檔案結構

### 後端檔案

#### 1. **Models/MapInfoConfig.cs** (新建)
地圖設定資料模型：
```csharp
public class MapInfoConfig
{
    public Dictionary<string, string> MapNameMapping { get; set; }
    public List<string> HideoutMapIds { get; set; }
 public List<string> NetherrealmMapIds { get; set; }
}

public class MapConfigItem
{
    public string MapId { get; set; }
    public string MapName { get; set; }
    public string MapType { get; set; }
}
```

#### 2. **mapInfo.json** (新建)
**位置：** `src/TorchLight.Statistics/mapInfo.json` (專案根目錄)

地圖設定檔案：
```json
{
  "mapNameMapping": {
    "XZ_YuJinZhiXiBiNanSuo200": "餘燼之息避難所",
    "GeBuLinCunLuo01": "隔壁林村落01",
    ...
  },
  "hideoutMapIds": ["XZ_YuJinZhiXiBiNanSuo200"],
  "netherrealmMapIds": [
    "GeBuLinCunLuo01",
    "YJ_TaiYangWangTing200",
    ...
  ]
}
```

**注意：**
- 檔案位於專案根目錄 `src/TorchLight.Statistics/`
- 建置時會自動複製到輸出目錄（bin/Debug 或 bin/Release）
- 執行時從輸出目錄讀取和寫入

#### 3. **MapMapper.cs** (重構)
主要變更：
- ✅ 從靜態資料改為從 JSON 載入
- ✅ 新增 `Initialize()` 方法初始化設定
- ✅ 新增 `LoadFromJson()` 載入設定
- ✅ 新增 `SaveToJson()` 儲存設定
- ✅ 新增 `StartFileWatcher()` 監控檔案變更
- ✅ 新增 `AddOrUpdateMapMapping()` 新增/更新地圖
- ✅ 新增 `DeleteMapMapping()` 刪除地圖
- ✅ 新增 `GetAllMapConfigs()` 取得所有設定
- ✅ 新增 `OnConfigUpdated` 事件通知設定更新
- ✅ 使用 `lock` 確保執行緒安全

#### 4. **UI/WebViewApi.cs** (擴充)
新增 API 方法：
```csharp
public string GetMapConfigs()     // 取得所有地圖設定
public string SaveMapConfig(...)        // 儲存地圖設定
public string DeleteMapConfig(...)      // 刪除地圖設定
```

#### 5. **Services/WebViewHub.cs** (擴充)
新增通知方法：
```csharp
public Task NotifyMapConfigUpdatedAsync(bool success, string message)
```

#### 6. **UI/MainWindow.cs** (修改)
- ✅ 註冊 `MapMapper.OnConfigUpdated` 事件
- ✅ 新增 `HandleMapConfigUpdated()` 處理方法

#### 7. **Program.cs** (修改)
- ✅ 啟動時呼叫 `MapMapper.Initialize()`

### 前端檔案

#### 1. **views/MapSettings.vue** (新建)
地圖設定管理頁面：

**功能：**
- 顯示所有地圖設定（依類型分類）
- 新增地圖對話框
- 編輯地圖對話框
- 刪除地圖確認
- 即時通知訊息
- 監聽後端設定更新通知

**介面區塊：**
- 🏠 藏身處地圖
- 🌌 異界地圖
- ❓ 未分類地圖

#### 2. **router/index.js** (修改)
新增路由：
```javascript
{
  path: '/settings',
  name: 'MapSettings',
  component: MapSettings
}
```

#### 3. **components/Header.vue** (修改)
- ✅ 新增「⚙️ 設定」導航連結
- ✅ 修正 logo emoji 編碼問題

## 資料流程

### 初始化流程
```
應用程式啟動
    ↓
Program.Main() 呼叫 MapMapper.Initialize()
    ↓
MapMapper.LoadFromJson()
    ↓
讀取 mapInfo.json
    ↓
成功：載入設定 / 失敗：使用預設設定
    ↓
啟動 FileSystemWatcher 監控檔案變更
```

### 設定更新流程（前端操作）
```
使用者在前端新增/編輯/刪除地圖
    ↓
呼叫 WebViewApi 方法
    ↓
MapMapper.AddOrUpdateMapMapping() / DeleteMapMapping()
    ↓
更新記憶體中的設定
    ↓
MapMapper.SaveToJson()
    ↓
寫入 mapInfo.json
    ↓
觸發 OnConfigUpdated 事件
    ↓
MainWindow.HandleMapConfigUpdated()
    ↓
WebViewHub.NotifyMapConfigUpdatedAsync()
    ↓
前端接收 'mapConfigUpdated' 訊息
    ↓
顯示通知 & 重新載入設定
```

### 檔案變更監控流程（外部編輯）
```
使用者用編輯器修改 mapInfo.json
    ↓
FileSystemWatcher 偵測到變更
    ↓
OnConfigFileChanged() 事件觸發
    ↓
防抖動：延遲 500ms
    ↓
備份當前設定
    ↓
MapMapper.LoadFromJson()
    ↓
成功：載入新設定 / 失敗：恢復備份
    ↓
觸發 OnConfigUpdated 事件
    ↓
通知前端
```

## UI 設計

### 地圖設定頁面佈局

```
┌──────────────────────────────────────────┐
│  地圖設定管理   [➕ 新增地圖]  │
├──────────────────────────────────────────┤
│  [通知訊息區域] │
├──────────────────────────────────────────┤
│  🏠 藏身處地圖          │
│  ┌─────────────┬─────────────┬─────────┐ │
│  │ 餘燼之息... │ ✏️ 🗑️      │     │ │
│  └─────────────┴─────────────┴─────────┘ │
├──────────────────────────────────────────┤
│  🌌 異界地圖       │
│  ┌─────────────┬─────────────┬─────────┐ │
│  │ 隔壁林村落  │ ✏️ 🗑️      │ 長明... │ │
│  │ 荊棘穢土  │ ✏️ 🗑️      │ 悲鳴... │ │
│  └─────────────┴─────────────┴─────────┘ │
└──────────────────────────────────────────┘
```

### 新增/編輯對話框

```
┌────────────────────────────────┐
│  新增地圖     [✕]  │
├────────────────────────────────┤
│  地圖 ID *        │
│  [GeBuLinCunLuo01___________]  │
│         │
│  地圖名稱 * │
│  [隔壁林村落01______________]  │
│          │
│  地圖類型 *        │
│  [▼ 異界地圖 ▼]         │
│  │
├────────────────────────────────┤
│[取消]    [儲存]   │
└────────────────────────────────┘
```

## 色彩設計

- **藏身處地圖**：綠色主題 `rgba(76, 175, 80, 0.1)`
- **異界地圖**：紫色主題 `rgba(156, 39, 176, 0.1)`
- **未分類地圖**：橙色主題 `rgba(255, 152, 0, 0.1)`
- **成功通知**：綠色 `#4caf50`
- **錯誤通知**：紅色 `#f44336`

## 錯誤處理

### 1. JSON 檔案格式錯誤
**情境：** `mapInfo.json` 格式不正確

**處理：**
```csharp
catch (Exception ex)
{
    Log.Error(ex, "載入地圖設定檔失敗，使用預設設定");
    LoadDefaultConfig();
    OnConfigUpdated?.Invoke(false, $"載入失敗: {ex.Message}");
}
```

### 2. 檔案寫入失敗
**情境：** 無法寫入 `mapInfo.json`（檔案鎖定或權限問題）

**處理：**
```csharp
catch (Exception ex)
{
    Log.Error(ex, "儲存地圖設定檔失敗");
    OnConfigUpdated?.Invoke(false, $"儲存失敗: {ex.Message}");
    return false;
}
```

### 3. 檔案監控更新失敗
**情境：** 檔案更新後重新載入失敗

**處理：**
```csharp
// 備份當前設定
var backupMapping = new Dictionary<string, string>(_mapNameMapping);
var backupHideout = new HashSet<string>(_hideoutMapIds);
var backupNetherrealm = new HashSet<string>(_netherrealmMapIds);

try
{
    LoadFromJson();
}
catch (Exception ex)
{
    // 恢復備份
    _mapNameMapping = backupMapping;
    _hideoutMapIds = backupHideout;
    _netherrealmMapIds = backupNetherrealm;

    OnConfigUpdated?.Invoke(false, $"設定檔更新失敗: {ex.Message}");
}
```

## 執行緒安全

所有存取共享資料的操作都使用 `lock` 保護：

```csharp
private static readonly object _lock = new();

lock (_lock)
{
    // 存取或修改共享資料
    _mapNameMapping[mapId] = mapName;
    // ...
}
```

## 防抖動機制

檔案監控使用防抖動避免短時間內重複載入：

```csharp
private static DateTime _lastReloadTime = DateTime.MinValue;
private static readonly TimeSpan _reloadDebounceTime = TimeSpan.FromSeconds(1);

var now = DateTime.Now;
if ((now - _lastReloadTime) < _reloadDebounceTime)
    return;

_lastReloadTime = now;
```

## 前端 API 使用

### 取得所有地圖設定
```javascript
const configs = await apiCall('GetMapConfigs')
// configs: Array<{ mapId, mapName, mapType }>
```

### 儲存地圖設定
```javascript
const result = await apiCall('SaveMapConfig', mapId, mapName, mapType)
// result: { success: boolean, message: string }
```

### 刪除地圖設定
```javascript
const result = await apiCall('DeleteMapConfig', mapId)
// result: { success: boolean, message: string }
```

### 監聽設定更新通知
```javascript
window.addEventListener('message', (event) => {
  const message = JSON.parse(event.data)
  
  if (message.type === 'mapConfigUpdated') {
    const { success, message: msg } = message.data
    // 顯示通知並重新載入設定
  }
})
```

## 使用方式

### 檔案位置說明

**開發時：**
- 原始檔案：`src/TorchLight.Statistics/mapInfo.json`
- 建置後會複製到：`src/TorchLight.Statistics/bin/Debug/net8.0-windows/mapInfo.json`

**執行時：**
- 應用程式會從執行檔所在目錄（輸出目錄）讀取 `mapInfo.json`
- 所有修改都會寫回輸出目錄的檔案
- **注意：** 如果要手動編輯，應該編輯輸出目錄中的檔案，或者編輯後重新建置

### 1. 新增地圖
1. 點擊「➕ 新增地圖」按鈕
2. 填寫地圖 ID（必填）
3. 填寫地圖名稱（必填）
4. 選擇地圖類型（藏身處/異界地圖）
5. 點擊「儲存」

### 2. 編輯地圖
1. 在地圖卡片上點擊「✏️」按鈕
2. 修改地圖名稱或類型（地圖 ID 無法修改）
3. 點擊「儲存」

### 3. 刪除地圖
1. 在地圖卡片上點擊「🗑️」按鈕
2. 確認刪除
3. 地圖設定將被移除

### 4. 手動編輯 JSON

**方法一：編輯輸出目錄的檔案（推薦）**
1. 找到執行檔所在目錄，例如：
   - Debug: `src/TorchLight.Statistics/bin/Debug/net8.0-windows/mapInfo.json`
   - Release: `src/TorchLight.Statistics/bin/Release/net8.0-windows/mapInfo.json`
2. 用文字編輯器開啟 `mapInfo.json`
3. 修改設定（注意 JSON 格式）
4. 儲存檔案
5. 應用程式將自動偵測並重新載入
6. 前端會顯示載入結果通知

**方法二：編輯原始檔案**
1. 編輯專案目錄中的 `src/TorchLight.Statistics/mapInfo.json`
2. 重新建置專案（檔案會複製到輸出目錄）
3. 啟動或重啟應用程式

**注意：**
- 如果應用程式正在執行，建議使用方法一
- 方法一的修改會立即生效（自動重新載入）
- 方法二需要重新建置和重啟應用程式
## 測試建議

### 1. 功能測試
- ✅ 新增不同類型的地圖
- ✅ 編輯地圖名稱和類型
- ✅ 刪除地圖
- ✅ 驗證 JSON 檔案內容正確

### 2. 錯誤處理測試
- ✅ 刪除 `mapInfo.json` 後啟動應用程式
- ✅ 將 `mapInfo.json` 改為無效的 JSON 格式
- ✅ 鎖定 `mapInfo.json` 後嘗試儲存
- ✅ 在檔案監控期間修改 JSON 為無效格式

### 3. 並發測試
- ✅ 同時從前端和外部編輯器修改設定
- ✅ 快速連續修改設定
- ✅ 驗證資料一致性

### 4. UI/UX 測試
- ✅ 通知訊息顯示正確
- ✅ 地圖分類顯示正確
- ✅ 對話框操作流暢
- ✅ 即時更新功能正常

## 注意事項

1. **地圖 ID 唯一性**
   - 每個地圖 ID 必須唯一
   - 編輯時無法修改地圖 ID（避免衝突）

2. **JSON 格式**
   - 必須是有效的 JSON 格式
   - 使用 UTF-8 編碼
   - 支援中文字元

3. **檔案權限**
   - 確保應用程式有寫入權限
   - 檔案鎖定時無法儲存

4. **執行緒安全**
   - 所有資料存取都已加鎖保護
   - 可安全處理並發操作

5. **效能考量**
   - 檔案監控有 1 秒防抖動
   - 檔案更新延遲 500ms 載入

## 未來擴展

1. **匯入/匯出功能**
   - 匯出設定為 JSON 檔案
   - 從其他 JSON 檔案匯入設定

2. **批次操作**
   - 批次新增多個地圖
   - 批次刪除地圖

3. **設定驗證**
   - 檢查地圖 ID 格式
   - 檢查名稱重複

4. **歷史記錄**
   - 保存設定變更歷史
   - 支援復原/重做操作

5. **雲端同步**
   - 支援將設定同步到雲端
- 多裝置共享設定
