# 地圖設定同步問題修復

## 問題描述

前端地圖設定修改後，地圖記錄列表中的地圖名稱沒有同步更新，仍顯示舊的名稱。

## 問題原因

### 1. 資料流程分析

**原始流程：**
```
地圖設定修改
    ↓
MapMapper.SaveToJson()
    ↓
觸發 OnConfigUpdated 事件
↓
MainWindow.HandleMapConfigUpdated()
 ↓
WebViewHub.NotifyMapConfigUpdatedAsync()
    ↓
前端接收 mapConfigUpdated 訊息
    ↓
❌ 前端沒有處理這個訊息
```

**資料讀取：**
```
前端請求 GetMapRecords
    ↓
WebViewApi.GetMapRecords()
    ↓
返回 MapRecordModel.Name
    ↓
❌ Name 是建立記錄時固定的，不會自動更新
```

### 2. 根本原因

1. **前端未處理 mapConfigUpdated 訊息**
- `mapStore.js` 的 `handleBackendMessage` 沒有處理 `mapConfigUpdated` 事件
   - 地圖設定更新後，前端不知道要重新載入資料

2. **後端返回靜態資料**
   - `GetMapRecords()` 直接返回 `MapRecordModel.Name`
   - 這個 Name 是在記錄建立時從 MapMapper 取得的
   - 即使 MapMapper 的設定更新了，已存在的記錄中的 Name 不會改變

## 解決方案

### 1. 前端：添加 mapConfigUpdated 訊息處理

**檔案：** `src/TorchLight.Statistics/wwwroot-src/src/stores/mapStore.js`

**修改內容：**
```javascript
case 'mapConfigUpdated':
  // 地圖設定更新後，重新載入地圖記錄以更新地圖名稱
  console.log('Map config updated:', message.data)
  refreshRecords()
  refreshCurrentMap()
break
```

**說明：**
- 當接收到 `mapConfigUpdated` 訊息時
- 呼叫 `refreshRecords()` 重新載入地圖記錄列表
- 呼叫 `refreshCurrentMap()` 重新載入當前地圖資訊
- 這樣前端就能顯示最新的地圖名稱

### 2. 後端：即時從 MapMapper 取得地圖名稱

**檔案：** `src/TorchLight.Statistics/UI/WebViewApi.cs`

#### 2.1 修改 GetMapRecords()

**修改前：**
```csharp
.Select(r => new
{
    r.RecordId,
    r.Id,
    r.Name,  // ❌ 靜態資料，不會更新
    // ...
})
```

**修改後：**
```csharp
.Select(r => new
{
    r.RecordId,
    r.Id,
    Name = MapMapper.GetMapName(r.Id),  // ✅ 即時取得最新名稱
    // ...
})
```

#### 2.2 修改 GetMapRecordDetail()

**修改前：**
```csharp
var detail = new
{
    record.RecordId,
    record.Id,
    record.Name,  // ❌ 靜態資料
    // ...
};
```

**修改後：**
```csharp
var detail = new
{
    record.RecordId,
    record.Id,
    Name = MapMapper.GetMapName(record.Id),  // ✅ 即時取得
    // ...
};
```

#### 2.3 修改 GetCurrentMapInfo()

**修改前：**
```csharp
// 異界地圖
return JsonSerializer.Serialize(new
{
    IsInMap = true,
    MapType = "Netherrealm",
    MapName = currentRecord.Name,  // ❌ 靜態資料
    // ...
});
```

**修改後：**
```csharp
// 異界地圖
return JsonSerializer.Serialize(new
{
    IsInMap = true,
    MapType = "Netherrealm",
    MapName = MapMapper.GetMapName(currentRecord.Id),  // ✅ 即時取得
  // ...
});
```

## 資料流程（修復後）

### 設定修改流程

```
使用者在前端修改地圖設定
 ↓
apiCall('SaveMapConfig', mapId, mapName, mapType)
    ↓
WebViewApi.SaveMapConfig()
    ↓
MapMapper.AddOrUpdateMapMapping()
    ↓
MapMapper.SaveToJson()
    ↓
觸發 OnConfigUpdated 事件
    ↓
MainWindow.HandleMapConfigUpdated()
    ↓
WebViewHub.NotifyMapConfigUpdatedAsync()
    ↓
前端接收 'mapConfigUpdated' 訊息
    ↓
mapStore.handleBackendMessage()
    ↓
✅ refreshRecords() - 重新載入地圖記錄
✅ refreshCurrentMap() - 重新載入當前地圖
```

### 資料讀取流程

```
前端請求地圖記錄
    ↓
apiCall('GetMapRecords')
    ↓
WebViewApi.GetMapRecords()
    ↓
遍歷 MapRecords
    ↓
✅ 對每個記錄呼叫 MapMapper.GetMapName(r.Id)
    ↓
從最新的 mapInfo.json 取得地圖名稱
    ↓
返回包含最新名稱的資料
    ↓
前端顯示最新的地圖名稱
```

## 關鍵改進

### 1. 前端即時響應

**之前：**
- 地圖設定更新後，前端不知道
- 需要手動重新整理頁面

**現在：**
- 接收到 `mapConfigUpdated` 訊息後自動重新載入
- 使用者體驗更流暢

### 2. 後端動態查詢

**之前：**
```csharp
// 返回固定的名稱
r.Name  // 例如："隔壁林村落01"
```

**現在：**
```csharp
// 即時從 MapMapper 查詢
MapMapper.GetMapName(r.Id)  // 永遠返回最新的名稱
```

**優點：**
- 不需要修改 `MapRecordModel` 的資料
- 即使過去的記錄，也能顯示最新的地圖名稱
- 保持資料一致性

### 3. 執行緒安全

MapMapper 的 `GetMapName()` 方法使用 `lock` 保護：
```csharp
public static string GetMapName(string mapId)
{
    lock (_lock)
    {
        return _mapNameMapping.TryGetValue(mapId, out var name) ? name : mapId;
    }
}
```

確保在多執行緒環境下安全存取。

## 測試場景

### 場景 1：修改已存在地圖的名稱

**操作步驟：**
1. 打開地圖記錄列表，看到 "隔壁林村落01"
2. 進入地圖設定頁面
3. 修改 "GeBuLinCunLuo01" 的名稱為 "哥布林村落"
4. 儲存

**預期結果：**
- 前端自動重新載入地圖記錄
- 列表中所有 "隔壁林村落01" 都更新為 "哥布林村落"
- 無需手動重新整理頁面

### 場景 2：修改當前地圖的名稱

**操作步驟：**
1. 玩家正在 "長明宮城" 地圖中
2. 在前端修改地圖名稱為 "太陽王庭"
3. 儲存

**預期結果：**
- 當前地圖資訊即時更新
- 顯示 "太陽王庭"
- 拾取記錄中的地圖名稱也更新

### 場景 3：刪除地圖設定

**操作步驟：**
1. 刪除某個地圖設定
2. 查看歷史記錄

**預期結果：**
- 歷史記錄顯示地圖ID（因為找不到名稱映射）
- 例如："SD_ShouGuSiDi000"

## 技術細節

### 資料流向圖

```
┌─────────────┐
│  mapInfo.json│
└──────┬──────┘
    │
       │ File Watch
 ↓
┌─────────────┐
│  MapMapper  │ ← AddOrUpdateMapping()
│  (記憶體)│ ← DeleteMapping()
└──────┬──────┘
     │
       │ GetMapName(mapId)
↓
┌─────────────┐
│  WebViewApi │
│ GetMapRecords()
│ GetMapDetail()
│ GetCurrentMap()
└──────┬──────┘
       │
       │ JSON
       ↓
┌─────────────┐
│   前端 Vue  │
│  mapStore   │
└─────────────┘
```

### 效能考量

**每次呼叫 MapMapper.GetMapName()：**
- O(1) 時間複雜度（Dictionary 查詢）
- 有 lock 保護，但影響微乎其微
- 記憶體中的操作，非常快速

**不會造成效能問題：**
- 地圖記錄數量通常不會太多（幾百到幾千筆）
- 即時查詢比儲存重複資料更節省記憶體
- 確保資料始終一致

## 已修改的檔案

1. ✅ `src/TorchLight.Statistics/wwwroot-src/src/stores/mapStore.js`
   - 添加 `mapConfigUpdated` 訊息處理

2. ✅ `src/TorchLight.Statistics/UI/WebViewApi.cs`
   - `GetMapRecords()`: 即時查詢地圖名稱
   - `GetMapRecordDetail()`: 即時查詢地圖名稱
   - `GetCurrentMapInfo()`: 即時查詢地圖名稱

3. ✅ `docs/MAP_CONFIG_SYNC_FIX.md`（本文件）

## 驗證檢查清單

- [ ] 修改地圖名稱後，記錄列表自動更新
- [ ] 修改當前地圖名稱後，當前地圖資訊自動更新
- [ ] 地圖詳情頁面顯示最新名稱
- [ ] 首頁統計資訊正確
- [ ] 刪除地圖設定後，顯示地圖ID
- [ ] 無需手動重新整理頁面

## 後續優化建議

### 1. 快取機制（可選）

如果地圖記錄數量非常大，可以考慮添加快取：

```csharp
private static readonly Dictionary<string, (string name, DateTime cacheTime)> _nameCache = new();

public static string GetMapNameCached(string mapId)
{
    if (_nameCache.TryGetValue(mapId, out var cached))
{
        if ((DateTime.Now - cached.cacheTime).TotalMinutes < 5)
 {
        return cached.name;
        }
    }
    
    var name = GetMapName(mapId);
    _nameCache[mapId] = (name, DateTime.Now);
    return name;
}
```

但目前的實作已經足夠高效，不需要快取。

### 2. 批次查詢（可選）

如果需要查詢多個地圖名稱，可以提供批次方法：

```csharp
public static Dictionary<string, string> GetMapNames(IEnumerable<string> mapIds)
{
    lock (_lock)
    {
        return mapIds.ToDictionary(
  id => id,
            id => _mapNameMapping.TryGetValue(id, out var name) ? name : id
        );
    }
}
```

但目前的單次查詢已經非常快速。

### 3. 前端快取（可選）

前端可以快取地圖ID到名稱的映射：

```javascript
const mapNameCache = ref({})

const getMapName = (mapId) => {
  if (!mapNameCache.value[mapId]) {
    // 從後端取得
  }
  return mapNameCache.value[mapId]
}

// 監聽 mapConfigUpdated，清除快取
case 'mapConfigUpdated':
  mapNameCache.value = {}
  refreshRecords()
  break
```

但目前的實作已經足夠簡潔有效。

## 總結

**問題：** 地圖設定修改後，記錄列表不同步

**原因：**
1. 前端沒有處理 `mapConfigUpdated` 訊息
2. 後端返回靜態的 `MapRecordModel.Name`

**解決：**
1. ✅ 前端添加訊息處理，自動重新載入
2. ✅ 後端即時從 MapMapper 查詢最新名稱

**效果：**
- 地圖設定修改後，所有顯示立即更新
- 使用者體驗流暢
- 資料保持一致性
- 效能優秀

修復完成！🎉
