# InitBagProcessor 實作說明

## 概述

建立了 `InitBagProcessor` 類別，用於處理背包初始化流程，採用與 `OpenMapProcessor` 和 `ItemChangeProcessor` 相同的簡化單線程模式。

## 核心設計

### 事件模型
```csharp
// 初始化開始
public event Action<DateTime> OnInitStarted;

// 即時模式：每個物品初始化立即觸發
public event Action<ItemModel> OnItemInitialized;

// 彙整模式：初始化完成時一次提供所有物品
public event Action<InitBagEvent> OnInitCompleted;
```

### InitBagEvent 結構
```csharp
public class InitBagEvent
{
    public DateTime StartTime { get; set; }
    public DateTime CompleteTime { get; set; }
    public List<ItemModel> Items { get; } = [];
}
```

## 處理流程

```
HandleLine(line)
  ↓
CheckBagInitializationState()  // 使用 LineParser
    ↓
1. 第一次初始化行 (isFirstInit = true)
   → HandleInitStart()
   → 建立 InitBagEvent
   → 觸發 OnInitStarted
    ↓
2. 後續初始化行
   → TryParseInitItem()  // 使用 LineParser.GetItemData()
   → HandleItemInit()
   → 觸發 OnItemInitialized（即時）
   → 加入 Items（彙整）
    ↓
3. 非初始化行（初始化完成）
   → HandleInitComplete()
   → 設定 CompleteTime
   → 觸發 OnInitCompleted（包含所有物品）
   → 清空 _currentInitEvent
```

## 與原版的對比

### 原版（GameLogProcessor 直接處理）
```csharp
// 分散在 ProcessLine 中
var (isInitLine, shouldProcess, isComplete, isFirstInit) = _lineParser.CheckBagInitializationState(line);

if (isInitLine && shouldProcess)
{
    if (isFirstInit)
    {
      Log.Information("偵測到背包初始化，重置背包資料");
        _bagInventoryManager.Reset();
    }
    
    var itemData = _lineParser.GetItemData(line);
    _bagInventoryManager.InitializeBagItem(itemData);
    Log.Debug("初始化背包物品: {ItemName} x{Count}", itemData.Name, itemData.Num);
 return;
}

if (isComplete)
{
    Log.Information("背包初始化完成，共 {Count} 種物品", _bagInventoryManager.BagData.Count);
    _bagInventoryManager.PrintInitializedBag();
    OnBagSyncCompleted?.Invoke();
    return;
}
```

### 新版（InitBagProcessor）
```csharp
// 集中在專用處理器
_initBagProcessor.HandleLine(line);

// 透過事件處理
_initBagProcessor.OnInitStarted += HandleInitStart;
_initBagProcessor.OnItemInitialized += HandleItemInitialized;
_initBagProcessor.OnInitCompleted += HandleInitCompleted;
```

## 整合到 GameLogProcessor

### 1. 建構函式
```csharp
public GameLogProcessor(...)
{
    // 建立處理器
 _initBagProcessor = new InitBagProcessor(lineParser);
    
    // 註冊事件
    _initBagProcessor.OnInitStarted += HandleInitStart;
    _initBagProcessor.OnItemInitialized += HandleItemInitialized;
    _initBagProcessor.OnInitCompleted += HandleInitCompleted;
}
```

### 2. ProcessLine
```csharp
public void ProcessLine(string line)
{
    // 0. 已開啟日誌
    if (LineParser.IsLogOpenedMessage(line)) { ... }

    // 1. 處理背包初始化
_initBagProcessor.HandleLine(line);

    // 2. 登入開始
    if (LineParser.IsLoginStart(line))
    {
        _bagInventoryManager.Reset();
        _mapPickRecordManager.Reset();
  _initBagProcessor.Reset();  // 重置初始化處理器
        return;
    }

    // 3. 其他處理...
}
```

### 3. 事件處理方法
```csharp
private void HandleInitStart(DateTime startTime)
{
 Log.Information("偵測到背包初始化，重置背包資料");
    _bagInventoryManager.Reset();
}

private void HandleItemInitialized(ItemModel item)
{
    _bagInventoryManager.InitializeBagItem(item);
    Log.Debug("初始化背包物品: {ItemName} x{Count}", item.Name, item.Num);
}

private void HandleInitCompleted(InitBagEvent initEvent)
{
    Log.Information("背包初始化完成，共 {Count} 種物品", initEvent.Items.Count);
    _bagInventoryManager.PrintInitializedBag();
    OnBagSyncCompleted?.Invoke();
}
```

## 優勢

### 1. 關注點分離
- **原版**: 初始化邏輯分散在 GameLogProcessor 中，與其他邏輯混雜
- **新版**: 初始化邏輯集中在 InitBagProcessor，清晰獨立

### 2. 程式碼可讀性
```csharp
// 原版：多個 if 判斷和 return
if (isInitLine && shouldProcess) { ... return; }
if (isComplete) { ... return; }

// 新版：單一呼叫
_initBagProcessor.HandleLine(line);
```

### 3. 易於測試
```csharp
[Fact]
public void TestBagInitialization()
{
    var lineParser = new LineParser(itemTable);
    var processor = new InitBagProcessor(lineParser);
    var items = new List<ItemModel>();
    
    processor.OnItemInitialized += items.Add;
    
    processor.HandleLine("[...] BagMgr@:InitBagData PageId=102 SlotId=5 ConfigBaseId=5011 Num=15");
    processor.HandleLine("[...] BagMgr@:InitBagData PageId=102 SlotId=8 ConfigBaseId=5028 Num=3");
    processor.HandleLine("[...] SomeOtherLine");  // 觸發完成
    
  Assert.Equal(2, items.Count);
}
```

### 4. 狀態管理
```csharp
// 集中管理初始化狀態
public int InitializedItemCount => _currentInitEvent?.Items.Count ?? 0;
```

### 5. 統一架構
與其他 Processor 保持一致的設計模式：
- `OpenMapProcessor` - 開啟地圖
- `ItemChangeProcessor` - 物品變更
- `InitBagProcessor` - 背包初始化

## 公開 API

### 屬性
```csharp
int InitializedItemCount  // 已初始化的物品數量
```

### 方法
```csharp
void HandleLine(string line)  // 處理日誌行
void Reset()   // 重置狀態（登入時使用）
```

### 事件
```csharp
event Action<DateTime> OnInitStarted
event Action<ItemModel> OnItemInitialized
event Action<InitBagEvent> OnInitCompleted
```

## 日誌範例

### 輸入（日誌行）
```
[2024.01.15-10:30:45:123][ 12345] BagMgr@:InitBagData PageId=102 SlotId=5 ConfigBaseId=5011 Num=15
[2024.01.15-10:30:45:124][ 12345] BagMgr@:InitBagData PageId=102 SlotId=8 ConfigBaseId=5028 Num=3
[2024.01.15-10:30:45:125][ 12345] BagMgr@:InitBagData PageId=102 SlotId=12 ConfigBaseId=5053 Num=7
[2024.01.15-10:30:45:130][ 12345] SomeOtherLine
```

### 輸出（日誌）
```
[INFO] 背包初始化開始
[DEBUG] 初始化背包物品: 迴響 x15
[DEBUG] 初始化背包物品: 神聖碎片 x3
[DEBUG] 初始化背包物品: 鐵匠磨刀石 x7
[INFO] 背包初始化完成，共 3 種物品
```

## 處理器比較

| 特性 | OpenMapProcessor | ItemChangeProcessor | InitBagProcessor |
|------|------------------|---------------------|------------------|
| **用途** | 收集開啟地圖資訊 | 處理物品變更 | 處理背包初始化 |
| **區塊標記** | 特定字串 | ItemChange@ start/end | 連續的 InitBagData |
| **完成條件** | Token+MapId+Level 齊全 | 遇到 end 標記 | 遇到非初始化行 |
| **即時事件** | OnMapStart | OnItemChanged | OnItemInitialized |
| **彙整事件** | OnMapComplete | OnBlockEnded | OnInitCompleted |
| **依賴注入** | 無 | 無 | LineParser |

## 與 LineParser 的協同

```csharp
// InitBagProcessor 依賴 LineParser 的方法：
_lineParser.CheckBagInitializationState(line)  // 檢查初始化狀態
_lineParser.GetItemData(line)        // 解析物品資料
_lineParser.ResetInitializationState()    // 重置狀態
```

## 效能考量

### 記憶體使用
- **單一 InitBagEvent 實例** - 只在初始化期間存在
- **物品列表緩存** - List<ItemModel>，初始化完成後清空
- **無 Dictionary** - 不需要執行緒隔離，無額外開銷

### 時間複雜度
- **檢查狀態**: O(1)
- **解析物品**: O(1)
- **添加物品**: O(1) (List.Add)
- **完成處理**: O(1)

## 測試場景

### 單元測試
1. ✅ 正常初始化流程
2. ✅ 空初始化（無物品）
3. ✅ 多次初始化（登入-登出-登入）
4. ✅ 初始化中斷（異常行）
5. ✅ 忽略頁面（PageId 100, 101）

### 整合測試
1. ✅ 與 GameLogProcessor 整合
2. ✅ 與 BagInventoryManager 整合
3. ✅ 事件通知流程
4. ✅ 重置功能

## 遷移指南

### 從舊版遷移

#### 步驟 1: 移除舊代碼
```csharp
// 移除 GameLogProcessor.ProcessLine 中的：
❌ var (isInitLine, shouldProcess, isComplete, isFirstInit) = _lineParser.CheckBagInitializationState(line);
❌ if (isInitLine && shouldProcess) { ... }
❌ if (isComplete) { ... }
```

#### 步驟 2: 添加處理器
```csharp
// GameLogProcessor 建構函式
✅ _initBagProcessor = new InitBagProcessor(lineParser);
✅ _initBagProcessor.OnInitStarted += HandleInitStart;
✅ _initBagProcessor.OnItemInitialized += HandleItemInitialized;
✅ _initBagProcessor.OnInitCompleted += HandleInitCompleted;
```

#### 步驟 3: 調用處理器
```csharp
// GameLogProcessor.ProcessLine
✅ _initBagProcessor.HandleLine(line);
```

#### 步驟 4: 重置處理器
```csharp
// 登入時
✅ _initBagProcessor.Reset();
```

## 總結

`InitBagProcessor` 完成了背包初始化邏輯的重構，實現了：

1. **統一架構** - 與其他 Processor 保持一致
2. **關注點分離** - 初始化邏輯獨立管理
3. **事件驅動** - 靈活的即時和彙整模式
4. **易於測試** - 清晰的輸入輸出
5. **效能優化** - 單線程設計，無額外開銷

這使得 `GameLogProcessor` 更加清晰和易於維護，每個處理器專注於各自的職責。🎉
