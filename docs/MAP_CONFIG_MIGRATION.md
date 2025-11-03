# MapConfigItem → MapIdConfig 遷移文件

## 概述

本次遷移將地圖設定系統從使用 `string Id` 的 `MapConfigItem` 全面改為使用 `int Id` 的 `MapIdConfig`。

## 變更摘要

### 核心變更

| 項目 | 舊系統 (MapConfigItem) | 新系統 (MapIdConfig) |
|------|------------------------|----------------------|
| **資料類型** | `public class MapConfigItem` | `public class MapIdConfig` |
| **ID 類型** | `string Id` | `int Id` |
| **資料來源** | `mapInfo.json` + `DefaultMapConfigs` | `Seed/MapMapper.json` + `MapIdDictionary` |
| **檔案監控** | ? 支援 | ? 不支援（從種子檔案載入） |
| **前端編輯** | ? 支援 | ? 支援（記憶體內修改） |

---

## 檔案變更清單

### 後端變更

#### 1. **MapInfoMapper.cs** - 完全重構 ?

**主要變更**:
- ? 移除所有 `MapConfigItem` 相關程式碼
- ? 移除 `_mapConfigs` (List<MapConfigItem>)
- ? 移除 `ConfigFileWatcher<MapConfigItem>`
- ? 移除 `LoadFromJson()`, `SaveToJson()` 等檔案操作方法
- ? 只保留 `_mapIdConfig` (Dictionary<int, MapIdConfig>)
- ? 新增 `Initialize()` - 從 AppConfiguration 載入
- ? 所有方法改為使用 `int mapId` 參數
- ? `AddOrUpdateMapMapping(int, string, MapType)` - 記憶體內修改
- ? `DeleteMapMapping(int)` - 記憶體內修改
- ? `GetAllMapConfigs()` - 返回 `List<MapIdConfig>`
- ? `GetAllMapConfigsByType()` - 返回 `Dictionary<MapType, List<MapIdConfig>>`
- ? `ReloadConfigs()` - 重新從 AppConfiguration 載入

**移除的方法**:
```csharp
// ? 已移除
private static void LoadFromJson()
private static List<MapConfigItem> LoadConfigsFromFile(string)
private static void LoadDefaultConfig()
public static bool SaveToJson()
public static string ExtractMapId(string)
public static MapInfo GetMapInfo(string)
public static string GetMapNameByFullPath(string)
public static string GetMapName(string)
public static MapType GetMapType(string)
public static bool CheckMapType(string, MapType)
public static bool AddOrUpdateMapMapping(string, string, MapType)
public static bool DeleteMapMapping(string)
```

**新增/保留的方法**:
```csharp
// ? 新增/保留
public static void Initialize()
public static MapIdConfig GetMapInfo(int mapId)
public static string GetMapName(int mapId)
public static MapType GetMapType(int mapId)
public static bool CheckMapType(int mapId, MapType mapType)
public static bool AddOrUpdateMapMapping(int mapId, string mapName, MapType mapType)
public static bool DeleteMapMapping(int mapId)
public static List<MapIdConfig> GetAllMapConfigs()
public static Dictionary<MapType, List<MapIdConfig>> GetAllMapConfigsByType()
public static void ReloadConfigs()
```

---

#### 2. **WebViewApi.cs** - API 更新 ?

**變更**:
```csharp
// ? 舊版
public string SaveMapConfig(string mapId, string mapName, string mapType)
public string DeleteMapConfig(string mapId)

// ? 新版
public string SaveMapConfig(int mapId, string mapName, string mapType)
public string DeleteMapConfig(int mapId)
```

**GetMapConfigs()**: 返回類型自動從 `Dictionary<MapType, List<MapConfigItem>>` 變為 `Dictionary<MapType, List<MapIdConfig>>`

---

#### 3. **AppConfiguration.cs** - 移除舊配置 ?

**變更**:
```csharp
// ? 已移除
public static readonly List<MapConfigItem> DefaultMapConfigs = [...];

// ? 保留
public static Dictionary<int, MapIdConfig> MapIdDictionary { get; private set; } = [];
public static void LoadConfigData() // 從 Seed/MapMapper.json 載入
```

---

#### 4. **MapInfoConfig.cs** - 標記過時 ?

```csharp
/// <summary>
/// 地圖設定項目（已棄用，請使用 MapIdConfig）
/// </summary>
[Obsolete("此類別已棄用，請使用 MapIdConfig 代替（使用 int Id 而非 string Id）")]
public class MapConfigItem
{
    // ...existing code...
}
```

---

### 前端變更

#### 5. **MapSettingsPanel.vue** - 更新為 int ID ?

**主要變更**:
```javascript
// ? 舊版
const editingMap = ref({
  mapId: '',   // string
  mapName: '',
  mapType: 'Netherrealm'
})

// ? 新版
const editingMap = ref({
  mapId: 0,      // number
  mapName: '',
  mapType: 'Netherrealm'
})
```

**輸入框變更**:
```vue
<!-- ? 舊版 -->
<input v-model="editingMap.mapId"
       type="text"
       placeholder="例如: GeBuLinCunLuo01" />

<!-- ? 新版 -->
<input v-model.number="editingMap.mapId"
       type="number"
       placeholder="例如: 1061000"
       min="1" />
<div class="form-hint">請輸入地圖的數字 ID（例如: 1061000）</div>
```

**API 呼叫變更**:
```javascript
// ? 儲存地圖
const result = await apiCall(
  'SaveMapConfig',
  parseInt(editingMap.value.mapId),  // 確保為整數
  editingMap.value.mapName,
  editingMap.value.mapType
)

// ? 刪除地圖
const result = await apiCall(
  'DeleteMapConfig', 
  parseInt(map.mapId)  // 確保為整數
)
```

**驗證邏輯更新**:
```javascript
// ? 新增數字驗證
if (!editingMap.value.mapId || editingMap.value.mapId <= 0) {
  showNotification('error', '地圖 ID 必須是大於 0 的數字')
  return
}
```

---

## 資料流程變更

### 舊系統 (MapConfigItem)
```
應用程式啟動
  ↓
MapMapper.Initialize()
  ↓
讀取 mapInfo.json (string Id)
  ↓
啟動 FileSystemWatcher
  ↓
前端修改 → 寫入 mapInfo.json
  ↓
FileSystemWatcher 偵測 → 重新載入
```

### 新系統 (MapIdConfig)
```
應用程式啟動
  ↓
AppConfiguration.LoadConfigData()
  ↓
讀取 Seed/MapMapper.json (int Id)
  ↓
建立 MapIdDictionary
  ↓
MapInfoMapper.Initialize()
  ↓
載入 MapIdDictionary
  ↓
前端修改 → 記憶體內更新 _mapIdConfig
  ↓
? 不寫回檔案（記憶體內修改）
```

---

## 功能比較

| 功能 | 舊系統 (MapConfigItem) | 新系統 (MapIdConfig) | 說明 |
|------|------------------------|----------------------|------|
| **資料來源** | mapInfo.json | Seed/MapMapper.json | 種子資料檔案 |
| **ID 類型** | String | Integer | 整數更高效 |
| **檔案監控** | ? 支援 | ? 不支援 | 不需要，因為從種子檔載入 |
| **檔案持久化** | ? 自動儲存 | ? 不儲存 | 修改只在記憶體內 |
| **前端編輯** | ? 支援 | ? 支援 | 兩者都支援 |
| **重啟後保留** | ? 是 | ? 否 | 重啟後回到種子資料 |
| **批次載入** | ? 逐筆 | ? 批次 | 從 JSON 一次載入 |
| **效能** | 中等 | 高 | Dictionary 查詢更快 |

---

## 遷移影響分析

### ? 已完成
- [x] 後端 API 全面改用 `int mapId`
- [x] 前端輸入改為 `number` 類型
- [x] 資料驗證更新（數字 > 0）
- [x] 舊類別標記為 `[Obsolete]`
- [x] 移除檔案監控機制
- [x] 移除 JSON 檔案讀寫邏輯

### ?? 注意事項
1. **前端修改不持久化**: 修改地圖設定後，重啟應用程式會回到原始的 `Seed/MapMapper.json` 資料
2. **無檔案監控**: 外部編輯 `Seed/MapMapper.json` 不會自動重新載入（需要重啟或手動呼叫 `ReloadConfigs()`）
3. **ID 格式變更**: 舊的 string ID（如 "GeBuLinCunLuo01"）無法直接使用，需要轉換為 int ID（如 1061000）

### ?? 優點
- ? 效能更好（Dictionary<int> 查詢更快）
- ? 資料一致性（從單一種子檔案載入）
- ? 記憶體佔用更小（int 比 string 更省空間）
- ? 類型安全（避免 string 比較錯誤）
- ? 擴展性強（ID 範圍大，1-2147483647）

### ?? 缺點
- ? 前端修改不持久化（重啟後遺失）
- ? 無外部檔案監控（需手動重新載入）
- ? 舊系統 API 不相容（需要遷移現有程式碼）

---

## 測試建議

### 單元測試

```csharp
[Fact]
public void MapInfoMapper_GetMapName_ShouldReturnCorrectName()
{
    // Arrange
    AppConfiguration.LoadConfigData();
    MapInfoMapper.Initialize();
    
    // Act
    var mapName = MapInfoMapper.GetMapName(1061000);
    
    // Assert
    Assert.Equal("7-0 悲風林地", mapName);
}

[Fact]
public void MapInfoMapper_AddOrUpdateMapMapping_ShouldUpdateInMemory()
{
    // Arrange
    MapInfoMapper.Initialize();
    
    // Act
    var success = MapInfoMapper.AddOrUpdateMapMapping(9999999, "測試地圖", MapType.Netherrealm);
    var mapName = MapInfoMapper.GetMapName(9999999);
    
    // Assert
    Assert.True(success);
    Assert.Equal("測試地圖", mapName);
}
```

### 整合測試

1. **前端新增地圖**
   - 輸入 mapId: `8888888`
   - 輸入 mapName: `測試地圖`
   - 選擇 mapType: `Netherrealm`
   - 點擊儲存
   - ? 應該成功新增
   - ? 列表中應該顯示新地圖

2. **前端編輯地圖**
   - 選擇現有地圖
   - 修改名稱
   - 點擊儲存
   - ? 應該成功更新
   - ? 列表中顯示新名稱

3. **前端刪除地圖**
 - 選擇現有地圖
   - 點擊刪除
   - 確認刪除
   - ? 應該成功刪除
   - ? 列表中不再顯示

4. **重啟測試**
   - 新增測試地圖
   - 重啟應用程式
   - ? 測試地圖應該消失（回到種子資料）

---

## 相容性說明

### 舊程式碼遷移

如果有其他程式碼使用舊的 `MapConfigItem`，需要進行以下變更：

```csharp
// ? 舊版程式碼
var configs = MapInfoMapper.GetAllMapConfigs(); // List<MapConfigItem>
foreach (var config in configs)
{
    Console.WriteLine($"{config.Id} - {config.Name}"); // string Id
}

// ? 新版程式碼
var configs = MapInfoMapper.GetAllMapConfigs(); // List<MapIdConfig>
foreach (var config in configs)
{
    Console.WriteLine($"{config.Id} - {config.Name}"); // int Id
}
```

### API 遷移

```csharp
// ? 舊版 API
MapInfoMapper.AddOrUpdateMapMapping("GeBuLinCunLuo01", "測試", MapType.Netherrealm);

// ? 新版 API
MapInfoMapper.AddOrUpdateMapMapping(1061000, "測試", MapType.Netherrealm);
```

---

## 未來改進方向

1. **持久化機制** (可選)
   - 添加「匯出設定」功能，將記憶體內的修改匯出為 JSON
   - 添加「匯入設定」功能，從 JSON 匯入自訂地圖

2. **合併機制** (可選)
   - Seed 資料 + 自訂資料合併
   - 自訂資料覆蓋種子資料的同名地圖

3. **版本管理** (可選)
   - 追蹤 Seed 檔案版本
   - 自動遷移舊版資料結構

---

## 結論

? **遷移完成**: 所有 `MapConfigItem` 相關程式碼已全面替換為 `MapIdConfig`

? **向後相容**: 舊類別保留並標記為 `[Obsolete]`，避免編譯錯誤

? **效能提升**: 使用 `int Id` 和 `Dictionary` 提升查詢效能

?? **注意**: 前端修改不會持久化，重啟後回到種子資料

---

**文件版本**: 1.0  
**最後更新**: 2024/01/15  
**維護者**: TorchLight Statistics Team
