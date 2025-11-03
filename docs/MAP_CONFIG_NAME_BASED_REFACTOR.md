# 地圖設定重構 - 改為以名稱為主

## 變更概述

將地圖設定系統從「以 MapId 為主」改為「以 Name 為主」，支援一個地圖名稱對應多個 MapId，並優化設定檔案管理流程。

## 主要變更

### 1. 檔案位置調整

**變更前：**
- 只從 `Seed/MapMapper.json` 讀取（Hardcode）

**變更後：**
- 優先從執行目錄讀取 `MapMapper.json`
- 如果不存在，則從 `Seed/MapMapper.json` 複製
- 所有修改都儲存到執行目錄的 `MapMapper.json`

**實作位置：** `src\TorchLight.Statistics\Configuration\AppConfiguration.cs`

```csharp
// 檢查執行目錄是否有 MapMapper.json
private static string MapConfigFilePath => Path.Combine(AppContext.BaseDirectory, "MapMapper.json");
private static string SeedMapConfigFilePath => Path.Combine(AppContext.BaseDirectory, "Seed", "MapMapper.json");

public static void LoadConfigData()
{
    // 1. 如果執行目錄沒有檔案，從 Seed 複製
  if (!File.Exists(MapConfigFilePath) && File.Exists(SeedMapConfigFilePath))
    {
    File.Copy(SeedMapConfigFilePath, MapConfigFilePath);
}

    // 2. 從執行目錄載入
    LoadMapperFromJson(MapConfigFilePath);
}
```

### 2. 資料結構調整

**JSON 格式（MapMapper.json）：**

```json
[
  {
    "id": [1061000, 1071000, 1081000],
    "name": "雜蕪街區",
    "type": "Netherrealm"
  },
  {
    "id": [1061001, 1071001, 1081001],
    "name": "鳴沙村落",
    "type": "Netherrealm"
  }
]
```

**記憶體結構：**
- `AppConfiguration.MapIdDictionary`: `Dictionary<int, MapIdConfig>`  
  保持不變，用於快速查詢 MapId → MapConfig

**新增輔助類別：**

```csharp
public class MapNameGroup
{
    public string Name { get; set; }
    public MapType Type { get; set; }
    public List<int> MapIds { get; set; }
}
```

### 3. API 變更

#### AppConfiguration

**新增方法：**
- `SaveMapperToJson()` - 儲存地圖設定到 MapMapper.json

**修改方法：**
- `LoadConfigData()` - 支援從執行目錄載入並自動複製 Seed 檔案

#### MapInfoMapper

**新增方法：**
```csharp
// 基於名稱的操作（推薦）
AddOrUpdateMapMappingByName(string mapName, List<int> mapIds, MapType mapType)
DeleteMapMappingByName(string mapName)
GetMapConfigsByNameGrouped() // 返回 Dictionary<MapType, List<MapNameGroup>>
```

**保留相容方法：**
```csharp
// 單一 ID 操作（相容舊版，內部轉換為名稱操作）
AddOrUpdateMapMapping(int mapId, string mapName, MapType mapType)
DeleteMapMapping(int mapId)
```

#### WebViewApi

**變更方法簽名：**

```csharp
// 變更前
public string SaveMapConfig(int mapId, string mapName, string mapType)
public string DeleteMapConfig(int mapId)

// 變更後
public string SaveMapConfig(string mapName, string mapIdsJson, string mapType)
public string DeleteMapConfig(string mapName)
```

**GetMapConfigs() 返回格式變更：**

```javascript
// 變更前
{
  "Netherrealm": [
 { "mapId": 1061000, "mapName": "雜蕪街區", "mapType": "Netherrealm" },
    { "mapId": 1071000, "mapName": "雜蕪街區", "mapType": "Netherrealm" }
  ]
}

// 變更後
{
  "Netherrealm": [
    {
      "name": "雜蕪街區",
      "type": "Netherrealm",
      "mapIds": [1061000, 1071000, 1081000]
    }
  ]
}
```

### 4. 前端 UI 變更

**檔案：** `src\TorchLight.Statistics\wwwroot-src\src\components\MapSettingsPanel.vue`

**主要變更：**

1. **卡片顯示改為以名稱為主**
 ```vue
   <div v-for="map in items" :key="map.name">
   <div class="map-name">{{ map.name }}</div>
     <span class="map-ids">ID: 1061000, 1071000... (共 3 個)</span>
   </div>
   ```

2. **新增/編輯對話框**
   ```vue
   <!-- 地圖名稱 -->
   <input v-model="editingMap.name" />
   
   <!-- 地圖 ID 列表（多行輸入） -->
   <textarea v-model="editingMap.mapIdsText" rows="6"></textarea>
   ```

3. **儲存邏輯**
   ```javascript
   // 解析多行 MapIds
   const mapIds = editingMap.value.mapIdsText
     .split('\n')
     .map(line => parseInt(line.trim()))
     .filter(id => !isNaN(id) && id > 0)

   await apiCall('SaveMapConfig', mapName, JSON.stringify(mapIds), mapType)
 ```

4. **刪除確認提示**
   ```javascript
   if (!confirm(`確定要刪除地圖「${map.name}」嗎？\n這將會刪除 ${map.mapIds.length} 個相關的地圖 ID。`))
   ```

## 資料流程

### 系統啟動流程

```
Program.Main()
  ↓
AppConfiguration.LoadConfigData()
  ↓
檢查執行目錄的 MapMapper.json
  ├─ 存在 → 直接載入
  └─ 不存在 → 從 Seed/MapMapper.json 複製
  ↓
LoadMapperFromJson() → 填充 MapIdDictionary
  ↓
MapInfoMapper.Initialize() → 引用 MapIdDictionary
```

### 前端新增/修改流程

```
使用者輸入地圖名稱 + 多個 MapId
  ↓
前端解析 MapIds (多行文字)
  ↓
apiCall('SaveMapConfig', name, JSON.stringify(mapIds), type)
  ↓
WebViewApi.SaveMapConfig()
  ↓
MapInfoMapper.AddOrUpdateMapMappingByName()
  ├─ 更新記憶體 MapIdDictionary
  └─ AppConfiguration.SaveMapperToJson()
      └─ 按名稱分組，寫入 MapMapper.json
  ↓
觸發 OnConfigUpdated 事件
  ↓
前端重新載入地圖列表
```

### 前端刪除流程

```
使用者刪除地圖「雜蕪街區」
  ↓
apiCall('DeleteMapConfig', mapName)
  ↓
WebViewApi.DeleteMapConfig()
  ↓
MapInfoMapper.DeleteMapMappingByName()
  ├─ 刪除所有相同名稱的 MapId
  └─ AppConfiguration.SaveMapperToJson()
  ↓
前端重新載入
```

## JSON 檔案同步機制

### 儲存邏輯

當 `AddOrUpdateMapMappingByName()` 或 `DeleteMapMappingByName()` 被呼叫時：

1. 更新記憶體中的 `MapIdDictionary`
2. 立即呼叫 `AppConfiguration.SaveMapperToJson()`
3. `SaveMapperToJson()` 將 `MapIdDictionary` 按名稱分組，重建 JSON 結構

```csharp
public static bool SaveMapperToJson()
{
    // 將 MapIdDictionary 按 Name 和 Type 分組
    var mapperItems = MapIdDictionary.Values
        .GroupBy(m => new { m.Name, m.Type })
        .Select(g => new MapperItem
        {
    Id = g.Select(m => m.Id).OrderBy(id => id).ToList(),
     Name = g.Key.Name,
  Type = g.Key.Type
        })
        .OrderBy(m => m.Type)
        .ThenBy(m => m.Name)
        .ToList();

    var jsonContent = JsonSerializer.Serialize(mapperItems, _jsonOptions);
    File.WriteAllText(MapConfigFilePath, jsonContent);
    return true;
}
```

### 不需要檔案監控

因為：
1. 所有修改都透過前端 API 進行
2. 每次修改都會立即更新記憶體和檔案
3. 使用者不應手動編輯執行目錄的 `MapMapper.json`（應編輯 Seed 檔案後重新部署）

## 向後相容性

### API 相容

舊的單一 MapId 方法仍然保留並可用：

```csharp
// 舊方法（仍可用）
MapInfoMapper.AddOrUpdateMapMapping(1061000, "雜蕪街區", MapType.Netherrealm)
MapInfoMapper.DeleteMapMapping(1061000)

// 內部實作會自動轉換為名稱操作
AddOrUpdateMapMapping(int mapId, string mapName, MapType mapType)
{
    var existingIds = _mapIdConfig
     .Where(kvp => kvp.Value.Name == mapName)
    .Select(kvp => kvp.Key)
        .ToList();
  
    if (!existingIds.Contains(mapId))
        existingIds.Add(mapId);
    
    return AddOrUpdateMapMappingByName(mapName, existingIds, mapType);
}
```

### 資料遷移

如果系統原本有舊版的地圖設定：
1. 系統會自動從 `Seed/MapMapper.json` 複製新格式檔案
2. `MapIdDictionary` 在記憶體中維持原有結構（Map<int, MapIdConfig>）
3. 所有現有查詢方法（`GetMapInfo(mapId)`）不受影響

## 優點

### 1. 簡化管理

- **變更前：** 要修改「雜蕪街區」需要逐一修改 7 個 MapId 記錄
- **變更後：** 只需修改一個名稱項目，包含所有相關 MapId

### 2. 減少重複

```json
// 變更前（196 條記錄）
[
  { "id": 1061000, "name": "7-0 雜蕪街區", "type": "Netherrealm" },
  { "id": 1071000, "name": "7-1 雜蕪街區", "type": "Netherrealm" },
  { "id": 1081000, "name": "7-2 雜蕪街區", "type": "Netherrealm" },
  ...
]

// 變更後（28 條記錄）
[
  {
    "id": [1061000, 1071000, 1081000, ...],
    "name": "雜蕪街區",
    "type": "Netherrealm"
  },
  ...
]
```

### 3. 更直觀的 UI

前端顯示：
```
雜蕪街區
ID: 1061000, 1071000, 1081000... (共 7 個)
```

使用者更容易理解一個地圖名稱對應多個時刻/等級的關係。

### 4. 保持查詢效能

雖然以名稱為主進行管理，但記憶體中仍維持 `Dictionary<int, MapIdConfig>` 結構，確保：
- `GetMapInfo(mapId)` 查詢仍是 O(1)
- `GetMapName(mapId)` 查詢仍是 O(1)
- 不影響日誌解析效能

## 注意事項

### 1. 檔案位置

- **執行目錄：** `bin/Debug/net8.0-windows/MapMapper.json`（可修改）
- **種子目錄：** `Seed/MapMapper.json`（唯讀參考）

### 2. 名稱前綴處理

系統會在載入時自動為 Netherrealm 地圖加上等級前綴：

```csharp
// JSON 中的名稱
"name": "雜蕪街區"

// 記憶體中的名稱（根據 MapId 自動加前綴）
MapIdDictionary[1061000].Name = "7-0 雜蕪街區"
MapIdDictionary[1071000].Name = "7-1 雜蕪街區"
MapIdDictionary[1121000].Name = "U8 幽邃的雜蕪街區"
```

### 3. MapId 範圍規則

系統會根據 MapId 範圍自動判斷前綴：

```csharp
if (id > 1120000)
    prefix = "幽邃的";
else if (id > 1090000)
    prefix = idStr switch
    {
        "10" => "滾燙的",
        "11" => "徹骨的",
        "12" => "柔軟的",
        "13" => "漆黑的",
 "14" => "耀眼的",
        _ => string.Empty,
    };
```

### 4. 前端驗證

前端會驗證輸入的 MapIds：
- 必須是數字
- 必須大於 0
- 至少需要一個有效 ID

## 測試建議

### 1. 功能測試

- ✅ 新增地圖（單一 MapId）
- ✅ 新增地圖（多個 MapIds）
- ✅ 編輯地圖名稱
- ✅ 編輯地圖 MapIds（新增/刪除）
- ✅ 刪除地圖
- ✅ 驗證 MapMapper.json 內容正確

### 2. 系統測試

- ✅ 首次啟動（執行目錄無檔案）
- ✅ 正常啟動（執行目錄有檔案）
- ✅ Seed 檔案缺失處理
- ✅ JSON 格式錯誤處理

### 3. 整合測試

- ✅ 修改地圖設定後，日誌解析是否正常
- ✅ 地圖類型判斷是否正確
- ✅ MapId → MapName 轉換是否正確
- ✅ 前端顯示是否符合預期

## 未來擴展

### 1. 批次匯入

可以支援從 CSV 或 Excel 批次匯入地圖設定：

```
名稱,MapIds,類型
雜蕪街區,"1061000,1071000,1081000",Netherrealm
鳴沙村落,"1061001,1071001,1081001",Netherrealm
```

### 2. 自動更新機制

可以從遠端伺服器定期下載最新的 `MapMapper.json` 種子檔案。

### 3. 歷史版本管理

保存 MapMapper.json 的歷史版本，支援復原操作。

## 相關檔案

### 後端

- `src\TorchLight.Statistics\Configuration\AppConfiguration.cs`
- `src\TorchLight.Statistics\Mapper\MapInfoMapper.cs`
- `src\TorchLight.Statistics\UI\WebViewApi.cs`
- `src\TorchLight.Statistics\Models\MapIdConfig.cs`

### 前端

- `src\TorchLight.Statistics\wwwroot-src\src\components\MapSettingsPanel.vue`

### 設定檔案

- `src\TorchLight.Statistics\Seed\MapMapper.json`（種子檔案）
- `bin\Debug\net8.0-windows\MapMapper.json`（執行目錄）
