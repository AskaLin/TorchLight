# OpenMapBlockProcessor 實作說明

## 概述

建立了 `OpenMapBlockProcessor` 類別，用於處理遊戲日誌中開啟地圖區塊的資訊收集，模式參考 `ItemChangeBlockProcessor`。

## 區塊範圍定義

### 開始標記
```
----Socket RecvMessage STT----Spv3Open----
```

### 結束標記
```
ItemChange@ ProtoName=Spv3Open start
```

### 區塊內需要收集的資訊
在開始與結束標記之間，需要收集以下三個資訊：
1. **MapToken** - 地圖標記
2. **MapId** - 地圖ID
3. **MapLevel** - 地圖等級

## 核心類別結構

### 1. OpenMapBlockContext
```csharp
public sealed class OpenMapBlockContext
{
    public bool InBlock { get; set; }            // 是否在區塊內
    public DateTime StartTime { get; set; }      // 區塊開始時間
    public string MapToken { get; set; }         // 地圖標記
    public int MapId { get; set; }       // 地圖ID
    public int MapLevel { get; set; }    // 地圖等級
    public List<MapInfoEvent> Buffer { get; }    // 事件緩存

    public bool IsComplete()      // 檢查三個資訊是否完整
    {
        return !string.IsNullOrEmpty(MapToken) && MapId > 0 && MapLevel > 0;
    }
}
```

### 2. 事件定義

#### OpenMapLogEvent (基類)
```csharp
public abstract record OpenMapLogEvent(DateTime Time, int ThreadId);
```

#### OpenMapBlockStarted (區塊開始)
```csharp
public record OpenMapBlockStarted(DateTime Time, int ThreadId) 
    : OpenMapLogEvent(Time, ThreadId);
```

#### OpenMapBlockEnded (區塊結束)
```csharp
public record OpenMapBlockEnded(DateTime Time, int ThreadId) 
    : OpenMapLogEvent(Time, ThreadId);
```

#### MapInfoEvent (地圖資訊)
```csharp
public record MapInfoEvent(
    DateTime Time,
    int ThreadId,
    string InfoType,    // "Token", "MapId", "Level"
    string Value
) : OpenMapLogEvent(Time, ThreadId);
```

### 3. OpenMapBlockProcessor

#### 主要事件
```csharp
// 區塊開始時觸發
public event Action<OpenMapBlockStarted> OnBlockStarted;

// 區塊內發生地圖資訊事件時立即觸發（即時模式）
public event Action<MapInfoEvent> OnMapInfoInsideBlock;

// 區塊結束時觸發，並提供該區塊內所有事件（彙整模式）
public event Action<OpenMapBlockEnded, IReadOnlyList<MapInfoEvent>> OnBlockEndedWithBatch;

// 🔥 當地圖資訊收集完成時觸發（包含 Token, MapId, MapLevel）
public event Action<OpenMapBlockContext> OnMapInfoComplete;
```

## 處理流程

### 1. 區塊開始處理
```
日誌行包含: "----Socket RecvMessage STT----Spv3Open----"
    ↓
TryParseBlockStart() 解析時間和 ThreadId
    ↓
HandleBlockStart()
    ├─ 建立或取得 Context
    ├─ 設定 InBlock = true
    ├─ 重置地圖資訊（Token, MapId, Level）
  ├─ 清空 Buffer
└─ 觸發 OnBlockStarted 事件
```

### 2. 地圖資訊處理（區塊內）
```
日誌行符合地圖資訊模式
    ↓
TryParseMapInfo() 
    ├─ 檢查是否在區塊內 (context.InBlock)
    ├─ 解析時間和 ThreadId
    └─ 識別資訊類型：
        ├─ LineParser.IsTokenLine() → MapToken
├─ LineParser.IsCurrentOpenMapIDLine() → MapId
        └─ LineParser.IsCurrentLevelLine() → MapLevel
    ↓
HandleMapInfo()
    ├─ 觸發 OnMapInfoInsideBlock 事件（即時模式）
    ├─ 加入 context.Buffer（彙整模式）
    └─ 記錄 Debug 日誌
```

### 3. 區塊結束處理
```
日誌行包含: "ItemChange@ ProtoName=Spv3Open start"
    ↓
TryParseBlockEnd() 解析時間和 ThreadId
    ↓
HandleBlockEnd()
    ├─ 檢查 Context 是否存在且在區塊內
    ├─ 設定 InBlock = false
    ├─ 觸發 OnBlockEndedWithBatch 事件（包含所有緩存事件）
    ├─ 檢查地圖資訊是否完整 (IsComplete())
    │   ├─ 完整 → 觸發 OnMapInfoComplete 事件 🔥
 │   └─ 不完整 → 記錄警告日誌
└─ 清空 Buffer
```

### 4. GameLogProcessor 整合
```csharp
// 建構函式中建立處理器
_openMapProcessor = new OpenMapBlockProcessor();

// 註冊事件處理
_openMapProcessor.OnMapInfoComplete += HandleMapInfoComplete;

// ProcessLine 中調用
_openMapProcessor.HandleLine(line);

// 處理地圖資訊完成
private void HandleMapInfoComplete(OpenMapBlockContext context)
{
    _mapPickRecordManager.SetMapToken(context.MapToken);
    _mapPickRecordManager.SetMapId(context.MapId);
    _mapPickRecordManager.SetMapLevel(context.MapLevel);

    if (_mapPickRecordManager.CurrentMapRecordInfoComplete())
    {
        _mapPickRecordManager.StartMapRecord(DateTime.Now);
        // 通知前端...
    }
}
```

## 日誌範例

### 完整區塊範例
```
[2024.01.15-10:30:45:123][ 12345] ----Socket RecvMessage STT----Spv3Open----
[2024.01.15-10:30:45:124][ 12345] Token=ABC123
[2024.01.15-10:30:45:125][ 12345] CurrentOpenMapID=1061000
[2024.01.15-10:30:45:126][ 12345] CurrentLevel=85
[2024.01.15-10:30:45:130][ 12345] ItemChange@ ProtoName=Spv3Open start
```

### 處理結果
```
[DEBUG] 開啟地圖區塊開始 [Thread 12345]
[DEBUG] 地圖資訊: Token=ABC123
[DEBUG] 地圖資訊: MapId=1061000
[DEBUG] 地圖資訊: Level=85
[DEBUG] 開啟地圖區塊結束 [Thread 12345] Token=ABC123, MapId=1061000, Level=85
[INFO] 地圖資訊收集完成: Token=ABC123, MapId=1061000, Level=85
[INFO] 地圖記錄已啟動: Token=ABC123, MapId=1061000, Level=85
```

## 與 ItemChangeBlockProcessor 的差異

| 特性 | ItemChangeBlockProcessor | OpenMapBlockProcessor |
|------|--------------------------|------------------------|
| **目標** | 處理背包物品變更 | 收集開啟地圖資訊 |
| **目標協議** | 多個 (Spv3Open, PickItem, PickItems, Push2) | 單一 (Spv3Open) |
| **區塊開始** | `ItemChange@ ProtoName=XXX start` | `----Socket RecvMessage STT----Spv3Open----` |
| **區塊結束** | `ItemChange@ ProtoName=XXX end` | `ItemChange@ ProtoName=Spv3Open start` |
| **資料收集** | BagModEvent (背包修改事件) | MapInfoEvent (地圖資訊) |
| **完成條件** | 區塊結束即完成 | Token + MapId + Level 三者齊全 |
| **特殊事件** | OnBagModInsideBlock | OnMapInfoComplete |

## 執行緒安全性

### Context 管理
- 使用 `Dictionary<int, OpenMapBlockContext>` 依 ThreadId 隔離
- 每個執行緒維護獨立的區塊狀態
- 支援多執行緒同時開啟不同地圖

### 注意事項
⚠️ 目前實作未加鎖，若多執行緒同時呼叫 `HandleLine` 可能有 race condition
- 建議在外層（GameLogProcessor）確保單執行緒存取
- 或在 HandleLine 內加入 `lock` 保護

## 保護性機制

### CloseStaleBlocks
```csharp
public void CloseStaleBlocks(TimeSpan timeout)
```

**用途：** 自動關閉超時的區塊，避免因遺漏 end 標記導致狀態異常

**時機：**
- 定期呼叫（例如每分鐘）
- 或在特定事件時（例如登入、切換角色）

**行為：**
1. 檢查所有 context 的 `InBlock` 狀態
2. 比較 `StartTime` 與當前時間
3. 超過 timeout → 自動觸發 `OnBlockEndedWithBatch` 和 `OnMapInfoComplete`（如果完整）

## 使用範例

### 基本使用
```csharp
var processor = new OpenMapBlockProcessor();

// 註冊事件
processor.OnMapInfoComplete += (context) =>
{
    Console.WriteLine($"地圖資訊完整: {context.MapToken}, {context.MapId}, {context.MapLevel}");
    // 啟動地圖記錄...
};

// 處理日誌行
processor.HandleLine(logLine);
```

### 進階使用（即時監控）
```csharp
processor.OnBlockStarted += (ev) =>
{
 Console.WriteLine($"開始收集地圖資訊 [Thread {ev.ThreadId}]");
};

processor.OnMapInfoInsideBlock += (ev) =>
{
    Console.WriteLine($"收到 {ev.InfoType}: {ev.Value}");
};

processor.OnBlockEndedWithBatch += (ev, events) =>
{
    Console.WriteLine($"收集完成，共 {events.Count} 個事件");
    foreach (var mapInfo in events)
    {
 Console.WriteLine($"  - {mapInfo.InfoType}: {mapInfo.Value}");
    }
};
```

### 超時保護
```csharp
// 定期檢查（例如每分鐘）
var timer = new Timer(_ => 
{
processor.CloseStaleBlocks(TimeSpan.FromMinutes(5));
}, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
```

## 錯誤處理

### 資訊不完整
```
[WARN] 地圖資訊不完整: Token=ABC123, MapId=0, Level=85
```
- 可能原因：日誌中缺少某些資訊行
- 處理方式：不會觸發 `OnMapInfoComplete`，等待下次完整區塊

### 區塊超時
```
[WARN] 自動關閉超時的開啟地圖區塊 [Thread 12345]
```
- 可能原因：遺漏結束標記或網路延遲
- 處理方式：自動關閉並嘗試觸發完成事件

### 找不到 Context
- 如果收到 end 但找不到對應的 context → 直接返回（忽略）
- 如果收到地圖資訊但不在區塊內 → 直接返回（忽略）

## 測試建議

### 單元測試場景
1. ✅ 完整區塊（start → Token → MapId → Level → end）
2. ✅ 不完整區塊（缺少 Token 或 MapId 或 Level）
3. ✅ 多執行緒並行（不同 ThreadId 同時開啟）
4. ✅ 超時關閉機制
5. ✅ 遺漏 start 標記（孤立的地圖資訊行）
6. ✅ 遺漏 end 標記（依賴超時機制）
7. ✅ 重複的 start（同一個 ThreadId）

### 整合測試場景
1. ✅ 與 GameLogProcessor 整合
2. ✅ 與 MapPickRecordManager 整合
3. ✅ 前端通知流程
4. ✅ 大量日誌行效能測試

## 優化建議

### 效能優化
- ✅ 使用 `Dictionary` 存儲 context（O(1) 查詢）
- ⚠️ 考慮定期清理不活躍的 context（記憶體優化）
- ⚠️ 大量日誌時，考慮非同步處理

### 可擴展性
- ✅ 支援新增更多地圖資訊類型（修改 `TryParseMapInfo`）
- ✅ 支援多種協議（修改區塊識別邏輯）
- ✅ 支援自訂超時時間

### 可維護性
- ✅ 清晰的事件命名和註解
- ✅ 分離解析邏輯和處理邏輯
- ✅ 統一的錯誤處理模式

## 相關檔案

### 核心檔案
- `src\TorchLight.Statistics\LogProcessor\OpenMapBlockProcessor.cs` - 主要處理器
- `src\TorchLight.Statistics\GameLogProcessor.cs` - 整合處理器
- `src\TorchLight.Statistics\LineRegex.cs` - Regex 模式定義

### 依賴檔案
- `src\TorchLight.Statistics\LineParser.cs` - 提供 IsTokenLine, IsCurrentOpenMapIDLine, IsCurrentLevelLine
- `src\TorchLight.Statistics\Services\MapPickRecordManager.cs` - 接收地圖資訊並啟動記錄
- `src\TorchLight.Statistics\Configuration\AppConfiguration.cs` - 時間格式和時區設定

## 總結

`OpenMapBlockProcessor` 提供了一個結構化、可靠的方式來收集開啟地圖時的關鍵資訊。透過區塊處理模式，確保了：

1. **資料完整性** - 只在三個資訊齊全時才啟動記錄
2. **執行緒隔離** - 支援多執行緒並行處理
3. **錯誤容忍** - 超時保護和不完整資料處理
4. **易於擴展** - 清晰的事件模型和處理流程
5. **統一架構** - 與 ItemChangeBlockProcessor 保持一致的設計模式

這個實作為後續的地圖記錄功能提供了堅實的基礎。🎉
