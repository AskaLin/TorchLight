# 物品設定重構 - 統一配置管理模式

## 變更概述

將物品設定管理模式統一為與地圖設定相同的模式，從執行目錄優先載入，支援自動複製種子檔案，並使用 Dictionary 結構提升查詢效能。

## 主要變更

### 1. 移除 DefaultItemConfigs 靜態列表

**變更前：**
```csharp
public static readonly List<ItemBaseModel> DefaultItemConfigs = [
    new() {Id = 1001, Name = "星星蛾火", Type= ItemType.DivinitySlate, ...},
    new() {Id = 1009, Name = "寰空神隙", Type= ItemType.DivinitySlate, ...},
    // ... 200+ 條記錄
];
```

**變更後：**
```csharp
public static Dictionary<int, ItemBaseModel> ItemIdDictionary { get; private set; } = [];
```

### 2. 統一檔案載入邏輯

#### AppConfiguration.cs

**新增方法：**

```csharp
// 檔案路徑
private static string ItemConfigFilePath => Path.Combine(AppContext.BaseDirectory, "ItemMapper.json");
private static string SeedItemConfigFilePath => Path.Combine(AppContext.BaseDirectory, "Seed", "ItemMapper.json");

// 載入物品設定
private static void LoadItemConfig()
{
    // 1. 檢查執行目錄是否有 ItemMapper.json
    if (!File.Exists(ItemConfigFilePath))
{
        // 2. 從 Seed 目錄複製
        if (File.Exists(SeedItemConfigFilePath))
    {
            File.Copy(SeedItemConfigFilePath, ItemConfigFilePath);
        }
    }
    
    // 3. 載入到 ItemIdDictionary
    LoadItemMapperFromJson(ItemConfigFilePath);
}

// 從 JSON 載入
private static void LoadItemMapperFromJson(string jsonFilePath)
{
 var jsonContent = File.ReadAllText(jsonFilePath);
    var itemList = JsonSerializer.Deserialize<List<ItemBaseModel>>(jsonContent, _jsonOptions);
    
    ItemIdDictionary.Clear();
    foreach (var item in itemList)
    {
        ItemIdDictionary[item.Id] = item;
    }
}

// 儲存到 JSON
public static bool SaveItemMapperToJson()
{
    var itemList = ItemIdDictionary.Values
      .OrderBy(i => i.Type)
        .ThenBy(i => i.Id)
      .ToList();
    
    var jsonContent = JsonSerializer.Serialize(itemList, _jsonOptions);
    File.WriteAllText(ItemConfigFilePath, jsonContent);
    
    return true;
}

// 查詢方法
public static ItemBaseModel GetItemInfo(int itemId)
{
    return ItemIdDictionary.TryGetValue(itemId, out var itemInfo) ? itemInfo : null;
}
```

### 3. 簡化 ItemInfoMapper

**變更前：**
- 自行載入 `ItemInfo.json`
- 使用 `AppConfiguration.DefaultItemConfigs` 作為預設值
- 維護自己的 `List<ItemBaseModel> _itemConfigs`

**變更後：**
- 直接引用 `AppConfiguration.ItemIdDictionary`
- 不再需要 `LoadDefaultConfig()`
- 改用 `Dictionary<int, ItemBaseModel>` 提升查詢效能

```csharp
public static void Initialize()
{
    lock (_lock)
    {
        try
        {
      // 從 AppConfiguration 載入物品ID字典（已由 Program.cs 中的 LoadConfigData() 載入）
     _itemConfigs = AppConfiguration.ItemIdDictionary;

          Log.Information("已載入物品設定: {ItemCount} 個物品", _itemConfigs.Count);
    OnConfigUpdated?.Invoke(true, "物品設定已成功載入");

  // 初始化檔案監控器（監控 ItemInfo.json，用於前端修改）
        if (File.Exists(ConfigFilePath))
            {
     _configWatcher = new ConfigFileWatcher<ItemBaseModel>(ConfigFilePath, LoadConfigsFromFile, OnConfigFileUpdated);
           _configWatcher.Initialize(_itemConfigs.Values.ToList());
        }
        }
        catch (Exception ex)
        {
       Log.Error(ex, "載入物品設定失敗");
 OnConfigUpdated?.Invoke(false, $"載入失敗: {ex.Message}");
        }
    }
}

// 改為使用 Dictionary 查詢
public static bool IsItemEnabled(int itemId)
{
    lock (_lock)
    {
        if (_itemConfigs.TryGetValue(itemId, out var config))
        {
return config.Enable;
        }
 return false;
    }
}
```

## 檔案結構對比

### 地圖設定

| 檔案位置 | 說明 |
|---------|------|
| `Seed/MapMapper.json` | 種子檔案（唯讀） |
| `MapMapper.json` | 執行目錄（可修改） |
| `AppConfiguration.MapIdDictionary` | 記憶體字典 |

### 物品設定

| 檔案位置 | 說明 |
|---------|------|
| `Seed/ItemMapper.json` | 種子檔案（唯讀） |
| `ItemMapper.json` | 執行目錄（可修改） |
| `AppConfiguration.ItemIdDictionary` | 記憶體字典 |
| `ItemInfo.json` | 前端編輯用（由 ItemInfoMapper 監控） |

## 資料流程

### 系統啟動流程

```
Program.Main()
  ↓
AppConfiguration.LoadConfigData()
  ├─ LoadMapConfig()
  │   ├─ 檢查執行目錄 MapMapper.json
  │   ├─ 不存在 → 從 Seed 複製
  │   └─ 載入到 MapIdDictionary
  │
  └─ LoadItemConfig()
      ├─ 檢查執行目錄 ItemMapper.json
 ├─ 不存在 → 從 Seed 複製
      └─ 載入到 ItemIdDictionary
  ↓
MapInfoMapper.Initialize()
  └─ _mapIdConfig = AppConfiguration.MapIdDictionary
  ↓
ItemInfoMapper.Initialize()
  └─ _itemConfigs = AppConfiguration.ItemIdDictionary
```

### 查詢流程對比

**地圖查詢：**
```csharp
// O(1) 查詢
var mapInfo = AppConfiguration.GetMapInfo(1061000);
// 或
if (AppConfiguration.MapIdDictionary.TryGetValue(1061000, out var info))
{
    Console.WriteLine(info.Name);
}
```

**物品查詢：**
```csharp
// O(1) 查詢
var itemInfo = AppConfiguration.GetItemInfo(5011);
// 或
if (AppConfiguration.ItemIdDictionary.TryGetValue(5011, out var item))
{
    Console.WriteLine(item.Name);
}
```

## 效能提升

### 查詢效能

**變更前（List<ItemBaseModel>）：**
```csharp
// O(n) 線性查詢
var config = _itemConfigs.FirstOrDefault(i => i.Id == itemId);
```

**變更後（Dictionary<int, ItemBaseModel>）：**
```csharp
// O(1) 雜湊查詢
if (_itemConfigs.TryGetValue(itemId, out var config))
```

### 記憶體使用

**變更前：**
- 靜態 `DefaultItemConfigs` (List) 約 200+ 條記錄
- ItemInfoMapper 自己的 `_itemConfigs` (List)
- 總計：兩份資料

**變更後：**
- `AppConfiguration.ItemIdDictionary` (Dictionary) 約 200+ 條記錄
- ItemInfoMapper 引用同一個 Dictionary
- 總計：一份資料

## JSON 格式

### ItemMapper.json（執行目錄）

```json
[
  {
    "Id": 1001,
    "Name": "星星蛾火",
    "Type": "DivinitySlate",
    "PageIdType": "Equipment",
    "Enable": true,
    "Watch": false,
    "Like": 0
  },
  {
    "Id": 5011,
  "Name": "遺忘之水",
    "Type": "Currency",
    "PageIdType": "Currency",
    "Enable": true,
    "Watch": false,
    "Like": 0
  }
]
```

### ItemInfo.json（前端編輯用）

ItemInfoMapper 仍然會監控 `ItemInfo.json`，這是為了支援前端編輯功能。當前端修改時：

1. 前端調用 API 更新 `ItemInfo.json`
2. `ConfigFileWatcher` 偵測到變更
3. 重新載入到 `ItemInfoMapper._itemConfigs`
4. 前端收到更新通知

## 優點

### 1. 統一管理模式

地圖和物品設定使用相同的載入邏輯：
- ✅ 檢查執行目錄
- ✅ 自動複製種子檔案
- ✅ 載入到 Dictionary
- ✅ 提供儲存方法

### 2. 提升查詢效能

從 `O(n)` 線性查詢改為 `O(1)` 雜湊查詢：

```csharp
// 變更前：需要遍歷整個 List
var item = _itemConfigs.FirstOrDefault(i => i.Id == itemId);  // O(n)

// 變更後：直接查詢 Dictionary
if (_itemConfigs.TryGetValue(itemId, out var item))  // O(1)
```

### 3. 減少程式碼重複

移除約 200 行的 `DefaultItemConfigs` 靜態初始化程式碼，改用 JSON 檔案管理。

### 4. 簡化維護

**新增物品：**
- 變更前：修改 C# 程式碼 → 重新編譯
- 變更後：編輯 `Seed/ItemMapper.json` → 複製到執行目錄

### 5. 支援熱更新

可以在系統運行時：
1. 修改執行目錄的 `ItemMapper.json`
2. 調用 `AppConfiguration.LoadItemConfig()`
3. 通知相關模組重新載入

## 注意事項

### 1. 檔案優先順序

```
執行目錄的 ItemMapper.json > Seed/ItemMapper.json
```

如果執行目錄已有檔案，不會覆蓋。

### 2. ItemInfo.json 的用途

`ItemInfo.json` 仍然保留用於前端編輯：
- 前端修改物品設定時寫入此檔案
- `ConfigFileWatcher` 監控此檔案變更
- 支援即時更新而不需重啟

### 3. 同步機制

目前 `ItemMapper.json` 和 `ItemInfo.json` 是獨立的：
- `ItemMapper.json`: 系統啟動載入
- `ItemInfo.json`: 前端編輯時使用

未來可考慮合併或建立同步機制。

### 4. 向後相容

所有現有的查詢方法仍然可用：
```csharp
// 這些方法仍然正常工作
ItemInfoMapper.GetItemName(itemId);
ItemInfoMapper.GetItemType(itemId);
ItemInfoMapper.IsItemEnabled(itemId);
```

## 測試建議

### 1. 功能測試

- ✅ 首次啟動（執行目錄無檔案）
- ✅ 正常啟動（執行目錄有檔案）
- ✅ Seed 檔案缺失處理
- ✅ JSON 格式錯誤處理
- ✅ 物品查詢效能測試

### 2. 整合測試

- ✅ ItemInfoMapper 初始化
- ✅ 物品名稱查詢
- ✅ 物品類型判斷
- ✅ Enable 狀態檢查
- ✅ 前端編輯功能

### 3. 效能測試

```csharp
// 測試查詢效能
var stopwatch = Stopwatch.StartNew();
for (int i = 0; i < 10000; i++)
{
    var item = AppConfiguration.GetItemInfo(5011);
}
stopwatch.Stop();
Console.WriteLine($"10000 次查詢耗時: {stopwatch.ElapsedMilliseconds}ms");
```

## 相關檔案

### 後端

- `src\TorchLight.Statistics\Configuration\AppConfiguration.cs` - 新增 ItemIdDictionary 和相關方法
- `src\TorchLight.Statistics\Mapper\ItemInfoMapper.cs` - 簡化載入邏輯，使用 ItemIdDictionary
- `src\TorchLight.Statistics\Models\ItemBaseModel.cs` - 資料模型（不變）

### 設定檔案

- `src\TorchLight.Statistics\Seed\ItemMapper.json` - 種子檔案（新增）
- `bin\Debug\net8.0-windows\ItemMapper.json` - 執行目錄（啟動時複製）
- `bin\Debug\net8.0-windows\ItemInfo.json` - 前端編輯用（保留）

## 未來擴展

### 1. 合併設定檔

可考慮將 `ItemMapper.json` 和 `ItemInfo.json` 合併：
- 統一使用一個檔案
- 簡化檔案管理
- 避免不一致問題

### 2. 設定版本控制

```json
{
  "version": "1.0.0",
  "items": [...]
}
```

### 3. 增量更新

只更新變更的部分，而非重新載入整個檔案。

### 4. 遠端同步

支援從遠端伺服器下載最新的物品設定。
