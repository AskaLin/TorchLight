# ?? 物品拾取流程文件

## ?? 目錄
1. [流程概述](#流程概述)
2. [完整流程圖](#完整流程圖)
3. [詳細步驟說明](#詳細步驟說明)
4. [未知物品處理機制](#未知物品處理機制)
5. [關鍵類別與方法](#關鍵類別與方法)
6. [資料流轉](#資料流轉)
7. [錯誤處理](#錯誤處理)
8. [測試場景](#測試場景)

---

## 流程概述

物品拾取流程是系統的核心功能之一，從檔案監聽到最終記錄，涉及多個組件的協同工作。整個流程分為以下幾個階段：

1. **日誌檔案監聽** - SafeFileTailWatcher 偵測新日誌行
2. **日誌行解析** - 識別物品變更區塊與事件
3. **背包更新** - 更新背包庫存狀態
4. **拾取記錄** - 記錄地圖拾取資訊
5. **前端通知** - 通知 WebView2 更新 UI

---

## 完整流程圖

```
┌──────────────────────────────────────────────────────────────────┐
│      1. 檔案監聽階段             │
└──────────────────────────────────────────────────────────────────┘
       ↓
        SafeFileTailWatcher
          (監聽 UE_game.log)
         ↓
      檔案變更 → OnNewLine 事件
                     ↓
┌──────────────────────────────────────────────────────────────────┐
│    2. 日誌解析階段    │
└──────────────────────────────────────────────────────────────────┘
   ↓
     GameLogProcessor.ProcessLine(line)
↓
              ┌───────────┴───────────┐
    │                  │
 ItemChangeBlockProcessor.HandleLine(line)
            │             │
  ├─ 區塊開始? ───────────┤
      │  (ItemChange start)   │
      │  │
       ├─ 背包修改? ───────────┤
 │  (Modfy BagItem)      │
    │     │
         └─ 區塊結束? ───────────┘
         (ItemChange end)
           ↓
      ┌───────────┴───────────┐
   │ 檢查 ProtoName:       │
         │ ? Spv3Open (開圖)     │
         │ ? PickItems (拾取)    │
     │ ? Push2 (收火)   │
          └───────────┬───────────┘
           ↓
       OnBagModInsideBlock 事件觸發
     ↓
┌──────────────────────────────────────────────────────────────────┐
│         3. 背包更新階段       │
└──────────────────────────────────────────────────────────────────┘
           ↓
        GameLogProcessor.HandleBagModification(BagModEvent)
   ↓
     ┌───────────┴───────────┐
        │    │
        BagInventoryManager.UpdateBagItem(ev)
      │              │
            ├─ 檢查 _itemTable ─────┤
            │  TryGetValue()   │
     │            │
  ├─ 新物品? ─────────────┤
         │  創建 PickedItemData  │
          │            │
        ├─ 現有物品? ───────────┤
        │  更新數量 & Slots   │
       │          │
            └───────────┬───────────┘
       ↓
         返回 ItemChangeResult
             (包含變化量、總計等)
         ↓
┌──────────────────────────────────────────────────────────────────┐
│       4. 開圖材料記錄 (Spv3Open)     │
└──────────────────────────────────────────────────────────────────┘
                     ↓
     ProtoName == "Spv3Open"?
      ↓
             檢查 _itemTable 取得 ItemType
        ↓
 ┌───────────┴───────────┐
      │ ItemType.MapTicket    │→ 記錄門票
            │ ItemType.BossTicket   │→ 記錄門票
    │ ItemType.GameplayTicket│→ 記錄門票
          │ ItemType.Compass      │→ 記錄羅盤
    │ ItemType.Probe   │→ 記錄探針
         │ ItemType.Currency     │→ 記錄迴響
       └───────────┬───────────┘
       ↓
        MapPickRecordManager.RecordMapMaterial()
          ↓
┌──────────────────────────────────────────────────────────────────┐
│            5. 拾取記錄階段 (PickItems)            │
└──────────────────────────────────────────────────────────────────┘
      ↓
        QuantityChange > 0 && ProtoName == "PickItems"?
               ↓
      YES → 進入記錄流程
       ↓
             ┌───────────┴───────────┐
       │ 檢查統計啟用狀態      │
  │ ItemInfoMapper. │
        │ IsItemEnabled()       │
   └───────────┬───────────┘
   ↓
已停用? → 跳過記錄 (返回 null)
      ↓
    已啟用? → 繼續
            ↓
 MapPickRecordManager.RecordPickedItem(configBaseId, slotId, quantityChange)
     ↓
    ┌───────────┴───────────┐
         │ 檢查是否在地圖中      │
           │ IsInMap == false?     │
     └───────────┬───────────┘
      ↓
     不在地圖 → 返回 null (不記錄)
  ↓
   在地圖中 → 繼續記錄
   ↓
      ┌───────────┴───────────┐
        │ 取得物品名稱          │
           │ _itemTable.│
           │ TryGetValue()         │
           └───────────┬───────────┘
   ↓
             ┌───────────┴───────────┐
          │ 找到? → 使用物品名稱  │
 │ 未找到? → 使用        │
 │ "未知的物品(ID)"      │
             └───────────┬───────────┘
    ↓
          ┌───────────┴───────────┐
         │ 更新拾取記錄 │
 │ _currentMapPickData   │
 │         │
    │ ? 新物品? 創建記錄    │
       │ ? 現有物品?           │
    │   - 新Slot? 添加      │
     │   - 現有Slot? 累加    │
   └───────────┬───────────┘
       ↓
       返回 MapPickResult
          (包含物品名稱、數量等)
     ↓
┌──────────────────────────────────────────────────────────────────┐
│        6. 日誌輸出階段   │
└──────────────────────────────────────────────────────────────────┘
  ↓
                ConsoleLogger.LogBagModification()
      ConsoleLogger.LogMapPickItem()
       ↓
                    輸出至 Serilog (Console/File)
           ↓
┌──────────────────────────────────────────────────────────────────┐
│         7. 前端通知階段     │
└──────────────────────────────────────────────────────────────────┘
       ↓
            WebViewHub.NotifyItemPickedAsync()
          WebViewHub.NotifyCurrentMapUpdateAsync()
   ↓
      透過 CoreWebView2 通知前端
 ↓
           前端更新 UI 顯示拾取資訊
```

---

## 詳細步驟說明

### 階段 1: 檔案監聽

**類別**: `SafeFileTailWatcher`

**機制**:
- **FileSystemWatcher**: 監聽檔案變更事件
- **輪詢機制**: 每 2 秒檢查一次檔案大小
- **防抖動**: 500ms 內的連續變更只處理一次

**觸發條件**:
- 檔案內容新增
- 檔案大小變化

**輸出**: 觸發 `OnNewLine` 事件，傳遞新的日誌行

---

### 階段 2: 日誌解析

**主要類別**: `ItemChangeBlockProcessor`

**關鍵機制**: 區塊識別系統

日誌中的物品變更被包裝在 `ItemChange` 區塊中：

```
[時間][線程ID] ItemChange start: PickItems
[時間][線程ID] Modfy BagItem Page = 100, Slot = 1, configbase_id = 12345, Num = 10
[時間][線程ID] ItemChange end: PickItems
```

**處理流程**:
1. **識別區塊開始** (`TryParseBlockStart`)
   - 檢查 ProtoName 是否在目標列表中 (`Spv3Open`, `PickItems`, `Push2`)
   - 建立或更新該線程的上下文 (`ItemChangeBlockContext`)

2. **識別背包修改** (`TryParseBagModification`)
   - 必須在區塊內才處理
 - 解析物品ID、Slot、數量等資訊
   - 立即觸發 `OnBagModInsideBlock` 事件

3. **識別區塊結束** (`TryParseBlockEnd`)
   - 觸發 `OnBlockEndedWithBatch` 事件
   - 清空該線程的緩衝區

**重要屬性**:
- `_targetProtocols`: 需要處理的協議名稱
  ```csharp
  ["Spv3Open", "PickItems", "Push2"]
  ```

---

### 階段 3: 背包更新

**類別**: `BagInventoryManager`

**方法**: `UpdateBagItem(BagModEvent ev)`

**處理邏輯**:

```csharp
public ItemChangeResult UpdateBagItem(BagModEvent ev)
{
    // 1. 取得物品名稱（處理未知物品）
    var result = new ItemChangeResult
    {
        ItemName = GetItemName(ev.ConfigBaseId), // ? 關鍵點
        ConfigBaseId = ev.ConfigBaseId,
        SlotId = ev.SlotId,
        NewSlotCount = ev.Num
    };

    // 2. 檢查是否為新物品
    if (!_bagData.TryGetValue(ev.ConfigBaseId, out var bagItem))
    {
  // 新物品 → 創建記錄
        var newItem = new PickedItemDataModel
        {
            BaseId = ev.ConfigBaseId,
    Name = result.ItemName,  // 可能是 "未知的物品(123)"
         Total = ev.Num
};
        newItem.Slots[ev.SlotId] = ev.Num;
        _bagData[ev.ConfigBaseId] = newItem;

        result.IsNewItem = true;
        result.QuantityChange = ev.Num;
        result.NewTotalCount = ev.Num;
        return result;
    }

    // 3. 更新現有物品
    result.PreviousTotalCount = bagItem.Total;

    if (bagItem.Slots.TryGetValue(ev.SlotId, out int previousSlotCount))
  {
        // 現有欄位 → 計算變化量
    int quantityChange = ev.Num - previousSlotCount;
        bagItem.Slots[ev.SlotId] = ev.Num;
        bagItem.Total += quantityChange;

        result.PreviousSlotCount = previousSlotCount;
        result.QuantityChange = quantityChange;
        result.NewTotalCount = bagItem.Total;
 }
    else
    {
   // 新欄位 → 直接添加
 bagItem.Slots[ev.SlotId] = ev.Num;
      bagItem.Total += ev.Num;

        result.IsNewSlot = true;
      result.QuantityChange = ev.Num;
        result.NewTotalCount = bagItem.Total;
    }

    return result;
}

private string GetItemName(int configBaseId)
{
  // ? 關鍵處理：未知物品命名
    return _itemTable.TryGetValue(configBaseId, out var item) 
        ? item.Name 
        : $"未知的物品({configBaseId})";
}
```

**返回值**: `ItemChangeResult`
- `ItemName`: 物品名稱（含未知物品處理）
- `QuantityChange`: 數量變化（正數=增加，負數=減少）
- `NewTotalCount`: 更新後的總數
- `IsNewItem`: 是否為新物品
- `IsNewSlot`: 是否為新欄位

---

### 階段 4: 開圖材料記錄

**觸發條件**: `ProtoName == "Spv3Open"`

**處理邏輯**:

```csharp
if (ev.ProtoName == "Spv3Open" && _itemTable.TryGetValue(ev.ConfigBaseId, out var item))
{
    // ? 注意：必須在 _itemTable 中才處理
    
    if (item.Type == ItemType.Currency)
    {
        // 記錄迴響數量
    Log.Debug("[開圖材料] 使用迴響數量 {res}", Math.Abs(bagResult.QuantityChange));
    }

    // 記錄各種開圖材料
    if (item.Type == ItemType.Compass || 
        item.Type == ItemType.Probe ||
        item.Type == ItemType.MapTicket || 
      item.Type == ItemType.BossTicket ||
        item.Type == ItemType.GameplayTicket || 
   item.Type == ItemType.Currency)
    {
        _mapPickRecordManager.RecordMapMaterial(ev.ConfigBaseId, item.Type);
    }
}
```

**重要**: 開圖材料記錄**不處理**未知物品，因為需要 `ItemType` 來判斷材料類型。

---

### 階段 5: 拾取記錄

**類別**: `MapPickRecordManager`

**方法**: `RecordPickedItem(int configBaseId, int slotId, int quantityChange)`

**完整處理流程**:

```csharp
public MapPickResult RecordPickedItem(int configBaseId, int slotId, int quantityChange)
{
    // 檢查點 1: 是否在地圖中
    if (!IsInMap)
    {
        return null;  // ? 不在地圖 → 不記錄
    }

    // 檢查點 2: 物品是否啟用統計
    if (!ItemInfoMapper.IsItemEnabled(configBaseId))
    {
        Log.Debug("[拾取統計] 物品 {ItemId} 已停用，跳過記錄", configBaseId);
        return null;  // ? 已停用 → 不記錄
    }

    // 檢查點 3: 取得物品名稱（處理未知物品）
    var result = new MapPickResult
    {
        ItemName = _itemTable.TryGetValue(configBaseId, out var item) 
        ? item.Name 
            : $"未知的物品({configBaseId})",  // ? 未知物品命名
        ConfigBaseId = configBaseId,
  SlotId = slotId,
        QuantityChange = quantityChange
    };

    // 更新記錄
  if (_currentMapPickData.TryGetValue(configBaseId, out var existingItem))
    {
        // 現有物品
        if (existingItem.Slots.TryGetValue(slotId, out int previousSlotCount))
        {
            // 現有欄位 → 累加
 existingItem.Slots[slotId] = previousSlotCount + quantityChange;
          existingItem.Total += quantityChange;

 result.PreviousSlotCount = previousSlotCount;
      result.NewSlotCount = existingItem.Slots[slotId];
 result.NewTotalCount = existingItem.Total;
            result.IsExistingSlot = true;
     }
        else
        {
            // 新欄位 → 添加
            existingItem.Slots[slotId] = quantityChange;
         existingItem.Total += quantityChange;

      result.NewSlotCount = quantityChange;
            result.NewTotalCount = existingItem.Total;
       result.IsNewSlot = true;
     }
    }
    else
    {
  // 新物品 → 創建記錄
        var newItem = new PickedItemDataModel
        {
            BaseId = configBaseId,
            Name = result.ItemName,  // 可能是 "未知的物品(123)"
      Total = quantityChange
      };
        newItem.Slots[slotId] = quantityChange;
        _currentMapPickData[configBaseId] = newItem;

        result.NewSlotCount = quantityChange;
        result.NewTotalCount = quantityChange;
        result.IsFirstTimeInMap = true;
    }

    return result;  // ? 成功記錄
}
```

**返回值**: `MapPickResult`（或 `null`）

**可能返回 `null` 的情況**:
1. 不在地圖中 (`IsInMap == false`)
2. 物品統計已停用 (`IsItemEnabled == false`)

---

## 未知物品處理機制

### ?? 概述

系統對於 `_itemTable` 中不存在的物品ID，採用**優雅降級**策略：
- ? **繼續處理**（不中斷流程）
- ? **顯示 ID**（方便除錯）
- ? **正常記錄**（數量、Slot 等資訊完整）

### ?? 處理層級

| 階段 | 處理方式 | 命名規則 | 是否繼續 |
|------|---------|---------|---------|
| **背包更新** | ? 完整處理 | `"未知的物品({ID})"` | ? 是 |
| **拾取記錄** | ? 完整處理 | `"未知的物品({ID})"` | ? 是 |
| **開圖材料** | ? 跳過記錄 | N/A | ? 是（但不記錄材料） |

### ?? 實作細節

#### 1. BagInventoryManager

```csharp
private string GetItemName(int configBaseId)
{
    return _itemTable.TryGetValue(configBaseId, out var item) 
        ? item.Name 
 : $"未知的物品({configBaseId})";  // ? 降級處理
}
```

**效果**:
```
[背包] 新增 未知的物品(99999) x10
[背包] 未知的物品(99999) 增加 5 (總計: 15)
```

#### 2. MapPickRecordManager

```csharp
var result = new MapPickResult
{
    ItemName = _itemTable.TryGetValue(configBaseId, out var item) 
     ? item.Name 
        : $"未知的物品({configBaseId})",  // ? 降級處理
    // ... 其他欄位
};
```

**效果**:
```
[地圖名稱] 拾取 未知的物品(99999) x3
[地圖名稱] 未知的物品(99999) +2 (總計: 5)
```

#### 3. 開圖材料記錄

```csharp
if (ev.ProtoName == "Spv3Open" && _itemTable.TryGetValue(ev.ConfigBaseId, out var item))
{
    // ? 必須在 _itemTable 中才處理
    // 未知物品不會進入此區塊
}
```

**原因**: 需要 `item.Type` 來判斷材料類型（門票/羅盤/探針/迴響）

**效果**: 未知物品在 Spv3Open 事件中會**更新背包**，但**不會記錄為開圖材料**

---

### ?? 範例場景

#### 場景 1: 拾取未知物品

**日誌**:
```
[2024/01/15 14:30:00][12345] ItemChange start: PickItems
[2024/01/15 14:30:00][12345] Modfy BagItem Page = 100, Slot = 1, configbase_id = 99999, Num = 10
[2024/01/15 14:30:00][12345] ItemChange end: PickItems
```

**處理結果**:

1. **背包更新** ?
   ```
   [背包] 新增 未知的物品(99999) x10
   ```

2. **拾取記錄** ?（如果在地圖中且啟用統計）
   ```
   [地圖名稱] 拾取 未知的物品(99999) x10
   ```

3. **資料庫狀態**:
   ```csharp
   // _bagData[99999]
   {
       BaseId: 99999,
       Name: "未知的物品(99999)",
       Total: 10,
 Slots: { 1: 10 }
   }
   
   // _currentMapPickData[99999]
   {
       BaseId: 99999,
       Name: "未知的物品(99999)",
  Total: 10,
       Slots: { 1: 10 }
   }
   ```

#### 場景 2: 使用未知開圖材料

**日誌**:
```
[2024/01/15 14:30:00][12345] ItemChange start: Spv3Open
[2024/01/15 14:30:00][12345] Modfy BagItem Page = 100, Slot = 1, configbase_id = 88888, Num = 0
[2024/01/15 14:30:00][12345] ItemChange end: Spv3Open
```

**處理結果**:

1. **背包更新** ?
   ```
   [背包] 未知的物品(88888) 減少 1 (總計: 0)
   ```

2. **開圖材料記錄** ?（因為不在 _itemTable 中）
   - 不會顯示在地圖記錄的材料欄位
   - 不會影響 `MapRecordModel.MapTicket/Compass/Probe`

---

### ??? 如何添加新物品

當發現 "未知的物品" 時，可以透過以下步驟添加：

#### 方法 1: 手動編輯 ItemInfo.json

1. 開啟 `ItemInfo.json`
2. 添加物品定義：
   ```json
   {
     "id": 99999,
     "name": "新物品名稱",
     "type": "Equipment",
     "enable": true,
   "like": 0,
     "pageIdType": "Bag"
   }
   ```
3. 儲存檔案（系統會自動重新載入）

#### 方法 2: 透過 WebView2 前端

1. 開啟物品管理介面
2. 點擊「新增物品」
3. 輸入物品資訊
4. 系統自動儲存並重新載入

---

### ?? 注意事項

1. **未知物品仍會被記錄**
   - 不會遺失拾取資訊
- 可以之後補充定義

2. **開圖材料例外**
   - 未知材料不會記錄在地圖資訊中
   - 建議優先定義常用的開圖材料

3. **統計功能依賴 Enable 屬性**
   - 即使在 _itemTable 中，`Enable = false` 也不會記錄拾取
   - 透過 `ItemInfoMapper.IsItemEnabled()` 檢查

4. **ID 顯示方便除錯**
 - 可以從日誌中快速找到缺少的物品ID
   - 方便批次添加定義

---

## 關鍵類別與方法

### GameLogProcessor

**職責**: 整合所有日誌處理邏輯

**核心方法**:
- `ProcessLine(string line)`: 處理單行日誌
- `HandleBagModification(BagModEvent ev)`: 處理背包修改事件

**關鍵決策點**:
```csharp
// 1. 檢查是否為拾取事件
if (bagResult.QuantityChange > 0 && ev.ProtoName == "PickItems")
{
    // 2. 記錄拾取
    var mapResult = _mapPickRecordManager.RecordPickedItem(...);
    
    // 3. 檢查是否成功記錄
    if (mapResult != null)
 {
        // 4. 輸出日誌與通知前端
    }
}
```

---

### ItemChangeBlockProcessor

**職責**: 識別和處理 ItemChange 區塊

**關鍵機制**: 多執行緒區塊管理

```csharp
private readonly Dictionary<int, ItemChangeBlockContext> _contexts = [];
```

每個執行緒維護自己的區塊狀態，避免混淆。

**事件**:
- `OnBlockStarted`: 區塊開始時觸發
- `OnBagModInsideBlock`: 背包修改時立即觸發（即時模式）
- `OnBlockEndedWithBatch`: 區塊結束時觸發（彙整模式）

---

### BagInventoryManager

**職責**: 管理背包庫存狀態

**資料結構**:
```csharp
private readonly Dictionary<int, PickedItemDataModel> _bagData = [];
```

**關鍵特性**:
- 按物品ID (`ConfigBaseId`) 索引
- 追蹤每個 Slot 的數量
- 計算總數量變化

---

### MapPickRecordManager

**職責**: 管理地圖拾取記錄

**核心資料**:
```csharp
private MapRecordModel _currentMapRecord = new();
private Dictionary<int, PickedItemDataModel> _currentMapPickData = [];
private readonly List<MapRecordModel> _mapRecords = [];
```

**生命週期**:
1. `StartMapRecord()`: 進入地圖時初始化
2. `RecordPickedItem()`: 拾取物品時記錄
3. `RecordMapMaterial()`: 開圖時記錄材料
4. `EndMapRecord()`: 離開地圖時結算並保存

---

## 資料流轉

### 物品資料模型

```
ItemModel (from _itemTable)
    ↓
PickedItemDataModel (in _bagData)
    ↓
MapPickResult (return value)
    ↓
前端 ViewModel
```

### 詳細欄位對應

#### ItemModel
```csharp
public class ItemModel
{
    public int ConfigBaseId { get; set; }  // 物品ID
    public string Name { get; set; }        // 物品名稱
    public ItemType Type { get; set; }      // 物品類型
}
```

#### PickedItemDataModel
```csharp
public class PickedItemDataModel
{
    public int BaseId { get; set; }         // 物品ID
    public string Name { get; set; }     // 物品名稱
    public int Total { get; set; }      // 總數量
    public Dictionary<int, int> Slots { get; set; }    // Slot → 數量
}
```

#### MapPickResult
```csharp
public class MapPickResult
{
    public string ItemName { get; set; }         // 物品名稱
    public int ConfigBaseId { get; set; }        // 物品ID
    public int SlotId { get; set; }     // Slot ID
    public int QuantityChange { get; set; }      // 數量變化
    public int PreviousSlotCount { get; set; }   // 該Slot原數量
    public int NewSlotCount { get; set; }        // 該Slot新數量
    public int NewTotalCount { get; set; }       // 物品總數量
    public bool IsFirstTimeInMap { get; set; }   // 第一次拾取
    public bool IsNewSlot { get; set; } // 新Slot
    public bool IsExistingSlot { get; set; }     // 現有Slot
}
```

---

## 錯誤處理

### 1. 檔案監聽失敗

**處理**: SafeFileTailWatcher 的雙重機制
```csharp
// FileSystemWatcher 失敗 → 輪詢機制接管
private async Task PollLoopAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
  {
        await Task.Delay(_pollInterval, ct);
        // 檢查檔案變化
    }
}
```

### 2. 日誌解析異常

**處理**: Try-Catch 包裹整個 ProcessLine
```csharp
try
{
    // 處理日誌
}
catch (Exception ex)
{
 Log.Error(ex, "處理日誌行時發生錯誤，日誌內容: {Line}", line);
}
```

### 3. 背包更新失敗

**處理**: 在 HandleBagModification 中捕捉
```csharp
try
{
    var bagResult = _bagInventoryManager.UpdateBagItem(ev);
    // ...
}
catch (Exception ex)
{
    Log.Error(ex, "處理背包修改時發生錯誤");
}
```

### 4. 未知物品處理

**處理**: 使用 `TryGetValue` 與降級命名
```csharp
var itemName = _itemTable.TryGetValue(configBaseId, out var item) 
 ? item.Name 
    : $"未知的物品({configBaseId})";
```

---

## 測試場景

### 場景 1: 正常拾取流程

**前置條件**:
- 玩家在異界地圖中
- 物品在 _itemTable 中
- 物品統計已啟用 (`Enable = true`)

**輸入日誌**:
```
[2024/01/15 14:30:00][12345] ItemChange start: PickItems
[2024/01/15 14:30:00][12345] Modfy BagItem Page = 100, Slot = 1, configbase_id = 1001, Num = 10
[2024/01/15 14:30:00][12345] ItemChange end: PickItems
```

**預期結果**:
1. 背包更新: 新增 10 個物品
2. 地圖記錄: 記錄拾取 10 個物品
3. 日誌輸出:
   ```
   [背包] 新增 測試物品 x10
   [測試地圖] 拾取 測試物品 x10
   ```
4. 前端通知: 顯示拾取訊息

---

### 場景 2: 未知物品拾取

**前置條件**:
- 玩家在異界地圖中
- 物品**不在** _itemTable 中

**輸入日誌**:
```
[2024/01/15 14:30:00][12345] ItemChange start: PickItems
[2024/01/15 14:30:00][12345] Modfy BagItem Page = 100, Slot = 1, configbase_id = 99999, Num = 10
[2024/01/15 14:30:00][12345] ItemChange end: PickItems
```

**預期結果**:
1. 背包更新: ? 新增 10 個物品（命名為 "未知的物品(99999)"）
2. 地圖記錄: ? 記錄拾取 10 個物品（如果啟用統計）
3. 日誌輸出:
   ```
   [背包] 新增 未知的物品(99999) x10
   [測試地圖] 拾取 未知的物品(99999) x10
   ```
4. 前端通知: ? 顯示 "未知的物品(99999)"

---

### 場景 3: 已停用物品拾取

**前置條件**:
- 玩家在異界地圖中
- 物品在 _itemTable 中
- 物品統計已停用 (`Enable = false`)

**輸入日誌**:
```
[2024/01/15 14:30:00][12345] ItemChange start: PickItems
[2024/01/15 14:30:00][12345] Modfy BagItem Page = 100, Slot = 1, configbase_id = 1001, Num = 10
[2024/01/15 14:30:00][12345] ItemChange end: PickItems
```

**預期結果**:
1. 背包更新: ? 新增 10 個物品
2. 地圖記錄: ? 不記錄（`RecordPickedItem` 返回 `null`）
3. 日誌輸出:
   ```
   [背包] 新增 測試物品 x10
   [拾取統計] 物品 1001 已停用，跳過記錄
   ```
4. 前端通知: ? 不通知

---

### 場景 4: 不在地圖中拾取

**前置條件**:
- 玩家在藏身處（不在異界地圖）
- 物品在 _itemTable 中且已啟用

**輸入日誌**:
```
[2024/01/15 14:30:00][12345] ItemChange start: PickItems
[2024/01/15 14:30:00][12345] Modfy BagItem Page = 100, Slot = 1, configbase_id = 1001, Num = 10
[2024/01/15 14:30:00][12345] ItemChange end: PickItems
```

**預期結果**:
1. 背包更新: ? 新增 10 個物品
2. 地圖記錄: ? 不記錄（`IsInMap = false`）
3. 日誌輸出:
   ```
   [背包] 新增 測試物品 x10
   ```
4. 前端通知: ? 不通知

---

### 場景 5: 累加拾取

**前置條件**:
- 玩家在異界地圖中
- 已經拾取過該物品（同一個 Slot）

**輸入日誌**:
```
// 第一次拾取
[2024/01/15 14:30:00][12345] Modfy BagItem Page = 100, Slot = 1, configbase_id = 1001, Num = 10

// 第二次拾取（同一個 Slot）
[2024/01/15 14:31:00][12345] Modfy BagItem Page = 100, Slot = 1, configbase_id = 1001, Num = 15
```

**預期結果**:
1. 第一次:
   ```
   [背包] 新增 測試物品 x10
   [測試地圖] 拾取 測試物品 x10
   ```

2. 第二次:
   ```
   [背包] 測試物品 增加 5 (總計: 15)
   [測試地圖] 測試物品 +5 (總計: 15)
   ```

3. 資料狀態:
   ```csharp
   _currentMapPickData[1001] = {
    Total: 15,
  Slots: { 1: 15 }
   }
   ```

---

### 場景 6: 多 Slot 拾取

**前置條件**:
- 同一物品拾取到不同 Slot

**輸入日誌**:
```
// Slot 1
[2024/01/15 14:30:00][12345] Modfy BagItem Page = 100, Slot = 1, configbase_id = 1001, Num = 10

// Slot 2
[2024/01/15 14:31:00][12345] Modfy BagItem Page = 100, Slot = 2, configbase_id = 1001, Num = 5
```

**預期結果**:
1. 第一次:
   ```
   [測試地圖] 拾取 測試物品 x10
   ```

2. 第二次:
   ```
   [測試地圖] 測試物品 +5 (總計: 15)
   ```

3. 資料狀態:
   ```csharp
   _currentMapPickData[1001] = {
       Total: 15,
       Slots: { 1: 10, 2: 5 }
   }
   ```

---

## 附錄: 相關配置

### 物品啟用統計設定

**檔案**: `ItemInfo.json`

```json
{
  "id": 1001,
  "name": "測試物品",
  "type": "Equipment",
  "enable": true,    // ← 控制是否記錄拾取
  "like": 5,
  "pageIdType": "Bag"
}
```

### 目標協議設定

**位置**: `ItemChangeBlockProcessor._targetProtocols`

```csharp
private readonly HashSet<string> _targetProtocols = 
[
    "Spv3Open",    // 開圖（扣除材料）
    "PickItems",   // 拾取物品
    "Push2"        // 收火
];
```

---

## 總結

### ? 拾取流程優點

1. **容錯性強**: 未知物品不會中斷流程
2. **資訊完整**: 所有數量、Slot 變化都被記錄
3. **除錯友善**: 未知物品顯示ID，方便追蹤
4. **多執行緒安全**: 每個執行緒維護獨立的區塊狀態
5. **可擴展性**: 易於添加新的物品定義

### ?? 關鍵要點

1. **未知物品會被記錄**，但命名為 `"未知的物品({ID})"`
2. **開圖材料**需要物品定義（因為需要 `ItemType`）
3. **統計啟用**通過 `Enable` 屬性控制
4. **地圖狀態**影響是否記錄拾取（`IsInMap`）
5. **區塊機制**確保正確關聯物品變更與協議

---

**文件版本**: 1.0  
**最後更新**: 2024/01/15  
**維護者**: TorchLight Statistics Team
